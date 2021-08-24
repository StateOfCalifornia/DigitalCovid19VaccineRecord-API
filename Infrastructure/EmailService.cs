using Application.Common.Interfaces;
using Application.Options;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class EmailService : IEmailService
    {
        private readonly SendGridClient _client;
        public readonly ILogger<SendGridClient> _logger;
        private readonly SendGridSettings _sgSettings;

        public EmailService(SendGridClient client, ILogger<SendGridClient> logger, SendGridSettings sgSettings)
        {
            _client = client;
            _logger = logger;
            _sgSettings = sgSettings;
        }

        #region IEmailService Implementation
        public void SendEmail(SendGridMessage message, string emailRecipient)
        {
            if (_sgSettings.SandBox != "0")
            {
                message.SetSandBoxMode(true);
            }
            _client.SendEmailAsync(message);
        }

        public async Task<bool> SendEmailAsync(SendGridMessage message, string emailRecipient)
        {
            if (_sgSettings.SandBox != "0")
            {
                message.SetSandBoxMode(true);
            }
            var resp = await _client.SendEmailAsync(message);
            return resp.IsSuccessStatusCode ;
        }
        #endregion

    }
}
