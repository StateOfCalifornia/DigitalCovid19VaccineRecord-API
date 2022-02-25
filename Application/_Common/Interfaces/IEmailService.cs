using System.Net;
using System.Threading.Tasks;
using SendGrid.Helpers.Mail;
using Application.Common.Models;
using System.Threading;

namespace Application.Common.Interfaces
{
    public interface IEmailService
    {
        Task<string> SendEmailAsync(EmailRequest message, CancellationToken cancellationToken);
    }
}
