using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IMessagingService
    {
        void SendMessage(string toPhoneNumber, string text, CancellationToken cancellationToken);
        Task<string> SendMessageAsync(string toPhoneNumber, string text, CancellationToken cancellationToken);
    }
}
