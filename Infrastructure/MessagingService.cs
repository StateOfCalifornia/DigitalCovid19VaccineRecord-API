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

namespace Infrastructure
{
    public class MessagingService : IMessagingService
    {
        private readonly ILogger<MessagingService> _logger;
        private readonly TwilioSettings _twilioSettings;
        
        #region Constructor
        public MessagingService(ILogger<MessagingService> logger, TwilioSettings twilioSettings)
        {
            _logger = logger;
            _twilioSettings = twilioSettings;
        }

        #endregion

        #region IMessageService Implementation

        public void SendMessage(string toPhoneNumber, string text, CancellationToken cancellationToken)
        {
            if(_twilioSettings.SandBox != "0")
            {
                return;
            }
            TwilioClient.Init(_twilioSettings.AccountSID, _twilioSettings.AuthToken);

            try
            {
                var message = MessageResource.CreateAsync(
                body: text,
                from: new Twilio.Types.PhoneNumber(_twilioSettings.FromPhone),
                to: new Twilio.Types.PhoneNumber(toPhoneNumber));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in texting. Message: " + ex.Message);
            }
        }

        public async Task<string> SendMessageAsync(string toPhoneNumber, string text, CancellationToken cancellationToken)
        {
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
                if(ex.Message.Contains("violates a blacklist rule.") || ex.Message.Contains("is not a mobile number") || ex.Message.Contains("is not a valid phone number") || ex.Message.Contains("SMS has not been enabled for the region indicated by the 'To' number"))
                {
                    response = "BADNUMBER";
                }
            }

            return response;
        }

        #endregion
    }
}