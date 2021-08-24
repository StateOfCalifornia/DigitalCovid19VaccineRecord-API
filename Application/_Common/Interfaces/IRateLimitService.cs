using Application.Common.Models;
using System;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IRateLimitService
    {
        Task<RateLimit> RateLimitAsync(string id, int maxCount, TimeSpan span);
    }
}
