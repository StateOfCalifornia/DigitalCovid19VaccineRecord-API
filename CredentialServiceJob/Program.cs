using System;
using Application.Common.Interfaces;
using Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Azure.Storage.Queues;
using System.Threading;
using Application.VaccineCredential.Queries.GetVaccineStatus;
using Newtonsoft.Json;
using Application.Common;
using System.Diagnostics;
using System.Collections.Generic;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using Snowflake.Data.Client;
using Application.Common.Models;
using System.Threading.Tasks;

namespace CredentialServiceJob
{

    public interface IQueueProcessor
    {
        void MainProcess();
    }

    public class Program : IQueueProcessor
    {
        private static ISnowFlakeService _snowFlakeService;
        private static IAesEncryptionService _aesEncryptionService;
        private static SnowFlakeSettings _snowFlakeSettings;
        private static AppSettings _appSettings;
        private static SendGridSettings _sendGridSettings;
        private static IMessagingService _messagingService;
        private static IEmailService _emailService;
        private static MessageQueueSettings _mqSettings;
        private static ILogger<Program> _logger;

        private static int messageCount = 0;
        private static int messageMinuteCount = 0;
        private static QueueClient queueClient;
        private static int taskCount = 0;
        

        public Program(KeySettings kSettings,TwilioSettings tSettings, ILogger<Program> logger,MessageQueueSettings mqSettings, ISnowFlakeService sfService, IAesEncryptionService encService, SnowFlakeSettings sfs, AppSettings appSettings, SendGridSettings sgs, IMessagingService ms, IEmailService emailService)
        {
            _snowFlakeService = sfService;
            _aesEncryptionService = encService;
            _snowFlakeSettings = sfs;
            _appSettings = appSettings;
            _sendGridSettings = sgs;
            _messagingService = ms;
            _emailService = emailService;
            _mqSettings = mqSettings;
            _logger = logger;

            _mqSettings.Validate();
            _snowFlakeSettings.Validate();
            _appSettings.Validate();
            _sendGridSettings.Validate();
            kSettings.Validate();
            tSettings.Validate();

        }

        public static void Main()
        {
            var services = new Startup().ConfigureService();
            var serviceProvider = services.BuildServiceProvider();
            var queueProcessor = serviceProvider.GetService<IQueueProcessor>();
            queueProcessor.MainProcess();
        }

        public void MainProcess()
        {
            if(Convert.ToInt32(_mqSettings.NumberThreads) <= 0)
            {
                return;
            }
            var qOptions = new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };
            
            queueClient = new QueueClient(_mqSettings.ConnectionString, _mqSettings.QueueName, qOptions);


            var timer = new Stopwatch();
            var prop = queueClient.GetProperties();
            var cnt = prop.Value.ApproximateMessagesCount;
            var loopCount = 0;
            _logger.LogInformation("Queue size is " + cnt);
            DateTime minuteStart = DateTime.Now;
            while (true)
            {
                loopCount++;
                try
                {   
                    if (loopCount % 10 == 1) {
                        _logger.LogInformation($"taskCount is {taskCount}  messages completed {messageCount}");
                    }

                    if (taskCount >= Convert.ToInt32(_mqSettings.NumberThreads))
                    {
                        _logger.LogInformation($"taskCount reached limit {taskCount}/{_mqSettings.NumberThreads}, sleeping {_mqSettings.SleepSeconds}");
                        Thread.Sleep(Convert.ToInt32(_mqSettings.SleepSeconds) * 2000);
                        return;
                    }

                    minuteStart = ThrottleSpeed(minuteStart, ref messageMinuteCount);

                    var messages = queueClient.ReceiveMessages(32,TimeSpan.FromSeconds(Convert.ToInt32(_mqSettings.InvisibleSeconds)));  //this message is invisible to all else for default of 30 seconds.
                    if (messages.Value.Length > 0)
                    {
                        if (!timer.IsRunning)
                        {
                            timer.Start();
                        }
                        var conn = new SnowflakeDbConnection();
                        conn.ConnectionString = _snowFlakeSettings.ConnectionString;
                        conn.Open();
                        var connThreadCount = new ConnectionThreadCount()
                        {
                            Connection = conn,
                            TaskCount = messages.Value.Length,
                        };

                        messageMinuteCount += messages.Value.Length;
                        Interlocked.Increment(ref taskCount);

                        for (int i = 0; i < messages.Value.Length; i++)
                        {
                            var message = messages.Value[i];
                            var objects = new object[3];
                            objects[0] = message;
                            objects[1] = queueClient;
                            objects[2] = connThreadCount;
                            Task.Run(() => ProcessMessage(objects));                            
                        }
                    }
                    else
                    {
                        if (taskCount == 0 && timer.IsRunning)
                        {
                            timer.Stop();
                            _logger.LogInformation($"TimerStopped: totalTime was {timer.Elapsed} messageCount:{messageCount} elapsedSeconds:{timer.ElapsedMilliseconds/1000} rate is { 60 * messageCount / (timer.ElapsedMilliseconds/1000)} per minute");
                        }
                        if (loopCount % 10 == 1)
                        {
                            if (timer.ElapsedMilliseconds > 0)
                            {
                                _logger.LogInformation($"totalTime was {timer.Elapsed} messageCount:{messageCount} elapsedSeconds:{timer.ElapsedMilliseconds / 1000} rate is { 60 * messageCount / (timer.ElapsedMilliseconds / 1000)} per minute");
                            }
                            else
                            {
                                _logger.LogInformation($"totalTime was {timer.Elapsed} messageCount:{messageCount} elapsedSeconds:{timer.ElapsedMilliseconds / 1000} rate is 0 per minute");
                            }
                        }
                        Thread.Sleep(Convert.ToInt32(_mqSettings.SleepSeconds) * 1000);
                    }
                }
                catch(Exception e)
                {
                    _logger.LogInformation("Error: " + e.Message);
                }
            }
        }

        private static DateTime ThrottleSpeed(DateTime minuteStart, ref int messageMinuteCount)
        {
            var nowTime = DateTime.Now;
            var minuteEnd = minuteStart.Add(TimeSpan.FromSeconds(60));
            if (nowTime < minuteEnd && messageMinuteCount > Convert.ToInt32(_mqSettings.MaxDequeuePerMinute))
            {
                var sleepTime = (int)(minuteEnd - nowTime).TotalSeconds;
                _logger.LogInformation($"ThrottleSpeed: Reached max dequeue limit, sleeping {sleepTime} seconds");
                Thread.Sleep(sleepTime * 1000);
                minuteStart = DateTime.Now;
                messageMinuteCount = 0;
            }
            else if(nowTime > minuteEnd)
            {
                _logger.LogInformation($"ThrottleSpeed: Throttle cleared messageMinuteCount:{messageMinuteCount} MaxDequePerMin:{_mqSettings.MaxDequeuePerMinute} timeintomin:{nowTime - minuteStart}");
                minuteStart = DateTime.Now;
                messageMinuteCount = 0;
            }
            return minuteStart;
        }

        private static async Task DeleteMessageIfNeeded(QueueMessage message, int processResult)
        {
            try
            {
                var ageHours = (DateTime.UtcNow - message.InsertedOn.Value).TotalHours;
                var failedProcess = false;
                var failedDelete = false;
                if ((processResult < 4 || processResult == 6) || ageHours > Convert.ToInt32(_mqSettings.MessageExpireHours))
                {
                    var res = await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
                    if (res.Status > 204)
                    {
                        _logger.LogInformation($"res.Status={res.Status}");
                        failedDelete = true;
                    }
                }
                else
                {
                    failedProcess = true;
                }
                if (failedDelete)
                {
                    _logger.LogInformation($"messageid:{message.MessageId} failed Delete:{failedDelete}  failedProcess:{failedProcess}");
                }
            }
            catch {}
        }

        public static async Task ProcessMessage(Object requestObject)
        {
            GetVaccineCredentialStatusQuery request = null;
            var arrayObject = (object[])requestObject;
            var message = (QueueMessage)arrayObject[0];
            var conn = (ConnectionThreadCount)arrayObject[2];
            var util = new Utils(_appSettings);
            int processResult = Int32.MaxValue;
            try
            {
                request = JsonConvert.DeserializeObject<GetVaccineCredentialStatusQuery>(message.Body.ToString());

                var cancellationToken = new CancellationToken();
                processResult = await util.ProcessStatusRequest(_logger, _emailService, _sendGridSettings, _messagingService, _aesEncryptionService, request, _snowFlakeService, conn.Connection, cancellationToken, message.DequeueCount);
            }
            catch(Exception e)
            {
                 _logger.LogError($"ProcessMessage exception:{e.Message} {e.StackTrace}");
            }

            await DeleteMessageIfNeeded(message, processResult);

            var count = Interlocked.Decrement(ref conn.TaskCount);

            if (count <= 0)
            {
                conn.Connection.Close();
                await conn.Connection.DisposeAsync();
                _logger.LogInformation($"STATUS ProcessedTotals.");
                Interlocked.Decrement(ref taskCount);
            }
            Interlocked.Increment(ref messageCount);
        }
    }
}
