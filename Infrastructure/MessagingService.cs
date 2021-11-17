using Application.Options;
using Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Threading;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Newtonsoft.Json;
using System.Net.Http;
using System.Dynamic;
using Newtonsoft.Json.Converters;
using Application.Common;

namespace Infrastructure
{
    public class MessagingService : IMessagingService
    {
        private readonly ILogger<MessagingService> _logger;
        private readonly TwilioSettings _twilioSettings;
        private readonly AppSettings _appSettings;
        private readonly CdphMessageSettings _cdphMessageSettings;
        private readonly CdphSmsMessagingClient _cdphSmsMessagingClient;
        private static int messageCount = 0;

        #region Constructor
        public MessagingService(ILogger<MessagingService> logger, TwilioSettings twilioSettings, AppSettings appSettings, CdphMessageSettings cdphMessageSettings, CdphSmsMessagingClient cdphSmsMessagingClient)
        {
            _logger = logger;
            _twilioSettings = twilioSettings;
            _appSettings = appSettings;
            _cdphMessageSettings = cdphMessageSettings;
            _cdphSmsMessagingClient = cdphSmsMessagingClient;
        }

        #endregion

        #region IMessageService Implementation       
        public async Task<string> SendMessageAsync(string toPhoneNumber, string text, string language, CancellationToken cancellationToken)
        {
            var currentMessageCount = Interlocked.Increment(ref messageCount);

            if (Utils.InPercentRange(currentMessageCount, Convert.ToInt32(_appSettings.UseCDPHMessagingService)))
            {
                _logger.LogInformation($"SENDMESSAGE_PINPOINT currentMessageCount: {currentMessageCount}");
                return await SendCdphMessageAsync(toPhoneNumber, text, language, cancellationToken);
            } 
            else
            {
                _logger.LogInformation($"SENDMESSAGE_TWILIO currentMessageCount: {currentMessageCount}");
                return await SendTwilioMessageAsync(toPhoneNumber, text, cancellationToken);
            }   
        }

        private async Task<string> SendTwilioMessageAsync(string toPhoneNumber, string text, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }
            if (_twilioSettings.SandBox != "0")
            {
                return "1";
            }

            string response = null;
            TwilioClient.Init(_twilioSettings.AccountSID, _twilioSettings.AuthToken);

            try
            {
                var message = await MessageResource.CreateAsync(
                body: text,
                from: new Twilio.Types.PhoneNumber(_twilioSettings.FromPhone),
                to: new Twilio.Types.PhoneNumber(toPhoneNumber));
                response = message.Sid;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in texting. Message: " + ex.Message);
                if (ex.Message.Contains("violates a blacklist rule.") || ex.Message.Contains("is not a mobile number") || ex.Message.Contains("is not a valid phone number") || ex.Message.Contains("SMS has not been enabled for the region indicated by the 'To' number"))
                {
                    response = "BADNUMBER";
                }
            }

            return response;
        }

        private async Task<string> SendCdphMessageAsync(string toPhoneNumber, string text, string language, CancellationToken cancellationToken)
        {
            if (_cdphMessageSettings.SandBox != "0")
            {
                return "1";
            }

            string response = null;
            
            try
            {
                var smsRequest = new SmsRequest
                {
                    ToPhoneNumber = toPhoneNumber,
                    Message = text,
                    Language = language
                };

                var requestBody = JsonConvert.SerializeObject(smsRequest);
                var message = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = _cdphSmsMessagingClient.SmsClient.BaseAddress,
                    Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
                };
                var sendResponse = await _cdphSmsMessagingClient.SmsClient.SendAsync(message, cancellationToken);

                var result = await sendResponse.Content.ReadAsStringAsync(cancellationToken);

                var smsResponse = JsonConvert.DeserializeObject<SmsResponse>(result);
                string deliveryStatus = null;

                if (sendResponse.IsSuccessStatusCode || sendResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    deliveryStatus = smsResponse.SmsResponseDetail.DeliveryStatus.ToUpper();
                }

                switch (deliveryStatus)
                {
                    case "SUCCESSFUL":
                        response = smsResponse.SmsResponseDetail.MessageId;
                        break;
                    case "NUMBER_IS_LANDLINE":
                    case "PERMANENT_FAILURE":
                    case "OPTED_OUT":
                        response = "BADNUMBER";  //Do not resend
                        break;
                    default:
                        break; //Try to resend
                }                         
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in texting. Message: " + ex.Message);               
            }
            return response;
            #endregion
        }

        private class SmsRequest
        {
            [JsonProperty("destination_number")]
            public string ToPhoneNumber { get; set; }
            [JsonProperty("sms_message_content")]
            public string Message { get; set; }
            [JsonProperty("locale")]
            public string Language { get; set; }
        }

        private class SmsResponseDetail
        {
            [JsonProperty("DeliveryStatus")]
            public string DeliveryStatus { get; set; }
            [JsonProperty("MessageId")]
            public string MessageId { get; set; }
            [JsonProperty("StatusCode")]
            public string StatusCode { get; set; }
            [JsonProperty("StatusMessage")]
            public string StatusMessage { get; set; }
        }

        private class SmsResponse
        {
            [JsonProperty("sms_response")]
            public SmsResponseDetail SmsResponseDetail { get; set; }
        }
    }
}