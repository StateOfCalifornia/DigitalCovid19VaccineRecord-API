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
        Task<string> SendMessageAsync(string toPhoneNumber, string text, string language, CancellationToken cancellationToken);
    }
}
