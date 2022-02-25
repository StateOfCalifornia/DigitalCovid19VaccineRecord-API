using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class EmailService : IEmailService
    {
        private readonly SendGridClient _client;
        private readonly ILogger<EmailService> _logger;
        private readonly SendGridSettings _sgSettings;
        private readonly PinpointEmailSettings _pinpointEmailSettings;
        private readonly PinpointEmailClient _pinpointEmailClient;
        private readonly AppSettings _appSettings;
        private static int messageCount = 0;

        public EmailService(SendGridClient client, ILogger<EmailService> logger, SendGridSettings sgSettings, PinpointEmailSettings pinpointEmailSettings, PinpointEmailClient pinpointEmailClient, AppSettings appSettings)
        {
            _client = client;
            _logger = logger;
            _sgSettings = sgSettings;
            _pinpointEmailSettings = pinpointEmailSettings;
            _pinpointEmailClient = pinpointEmailClient;
            _appSettings = appSettings;
        }

        #region IEmailService Implementation        

        public async Task<string> SendEmailAsync(EmailRequest message, CancellationToken cancellationToken)
        {
            var currentMessageCount = Interlocked.Increment(ref messageCount);

            if(Utils.InPercentRange(currentMessageCount, Convert.ToInt32(_appSettings.UsePinpointEmailService)))
            {
                _logger.LogInformation($"SENDEMAIL_PINPOINT currentMessageCount: {currentMessageCount}");
                return await SendPinpointEmailAsync(message, cancellationToken);
            }
            else
            {
                _logger.LogInformation($"SENDEMAIL_SENDGRID currentMessageCount: {currentMessageCount}");
                return await SendSendGridEmailAsync(message, cancellationToken);
            }            
        }
        
        private async Task<string> SendPinpointEmailAsync(EmailRequest message, CancellationToken cancellationToken)
        {
            if (_pinpointEmailSettings.SandBox != "0")
            {
                return "1";
            }

            string response = null;

            try
            {
                var emailRequest = new EmailContentRequest
                {
                    RecipientEmail = message.RecipientEmail,
                    RecipientName = message.RecipientName,                    
                    SubjectLine = message.Subject,
                    HtmlContent = message.HtmlContent,
                    PlainTextContent = message.PlainTextContent                    
                };

                var requestBody = JsonConvert.SerializeObject(emailRequest);

                var emailMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = _pinpointEmailClient.EmailClient.BaseAddress,
                    Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
                };

                var sendResponse = await _pinpointEmailClient.EmailClient.SendAsync(emailMessage, cancellationToken);
                
                switch (sendResponse.StatusCode)
                {
                    case HttpStatusCode.OK:
                        response = "SUCCESS";
                        break;
                    case HttpStatusCode.BadRequest:
                        response = "BAD_EMAIL"; //Do not resend
                        break;
                    case HttpStatusCode.TooManyRequests:  //THROTTLED
                    case HttpStatusCode.InternalServerError:  //TIMEOUT or TEMPORARY_FAILURE
                    default:
                        break;  //Resend
                }
            }
            catch(Exception ex)
            {
                _logger.LogError("Error in Pinpoint emailing. Message: " + ex.Message);
            }

            return response;
        }
        private async Task<string> SendSendGridEmailAsync(EmailRequest message, CancellationToken cancellationToken)
        {
            string response = null;

            var sendGridMessage = new SendGridMessage();
            
            if (_sgSettings.SandBox != "0")
            {
                sendGridMessage.SetSandBoxMode(true);
            }
            
            sendGridMessage.AddTo(message.RecipientEmail);
            sendGridMessage.SetFrom(_sgSettings.SenderEmail, _sgSettings.Sender);
            sendGridMessage.Subject = message.Subject;
            sendGridMessage.PlainTextContent = message.PlainTextContent;
            sendGridMessage.HtmlContent = message.HtmlContent;

            var resp = await _client.SendEmailAsync(sendGridMessage, cancellationToken);

            if (resp.IsSuccessStatusCode)
            {
                response = "SUCCESS";
            }
            return response;
        }
        #endregion

        private class EmailContentRequest
        {
            [JsonProperty("recipient_email")]
            public string RecipientEmail { get; set; }
            [JsonProperty("recipient_name")]
            public string RecipientName { get; set; }            
            [JsonProperty("subject")]
            public string SubjectLine { get; set; }
            [JsonProperty("html")]
            public string HtmlContent { get; set; }
            [JsonProperty("plaintext")]
            public string PlainTextContent { get; set; }
        }
    }
}
