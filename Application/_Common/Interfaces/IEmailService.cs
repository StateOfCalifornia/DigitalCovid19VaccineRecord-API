using System.Net;
using System.Threading.Tasks;
using SendGrid.Helpers.Mail;

namespace Application.Common.Interfaces
{
    public interface IEmailService
    {
        void SendEmail(SendGridMessage message, string recipient);

        Task<bool> SendEmailAsync(SendGridMessage message, string recipient);

    }
}
