using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IQueueService
    {
        Task<bool> AddMessageAsync(string message);
    }
}
