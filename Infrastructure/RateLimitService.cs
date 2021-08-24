using Application.Common.Interfaces;
using Application.Common.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    class CacheData
    {
        public DateTime DateAdded { get; set; }
        public int Count { get; set; }
        public DateTime ExpireDate { get; set; }
    }

    public class RateLimitService : IRateLimitService
    {
        private readonly ILogger<RateLimit> _logger;
        private readonly IDistributedCache _cache;
        public RateLimitService(IDistributedCache cache, ILogger<RateLimit> logger)
        {
            _logger = logger;
            _cache = cache;
        }


        public async Task<RateLimit> RateLimitAsync(string id, int maxCount, TimeSpan span)
        {
            RateLimit rateLimit = new RateLimit();
            DateTime now = DateTime.Now;
            rateLimit.Limit = maxCount;
            if(rateLimit.Limit < 0)
            {
                return rateLimit;
            }
            try
            {
                var cacheVal = await _cache.GetStringAsync(id);

                CacheData cacheData;
                if (cacheVal == null)
                {
                    //We just did this less that 1 minute ago....
                    cacheData = new CacheData
                    {
                        Count = 1,
                        DateAdded = now,
                        ExpireDate = now + span
                    };
                    rateLimit.TimeRemaining = span;
                }
                else
                {
                    cacheData = JsonConvert.DeserializeObject<CacheData>(cacheVal);
                    cacheData.Count++;
                    rateLimit.TimeRemaining = cacheData.ExpireDate - now;
                }
                rateLimit.Remaining = maxCount - cacheData.Count;
                if(rateLimit.TimeRemaining.TotalMilliseconds <= 0)
                {
                    now = DateTime.Now;
                    rateLimit.TimeRemaining = span;
                    rateLimit.Remaining = maxCount - 1;
                    cacheData.Count = 1;
                    cacheData.DateAdded = now;
                    cacheData.ExpireDate = now + span;
                }
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = rateLimit.TimeRemaining
                };
                await _cache.SetStringAsync(id, JsonConvert.SerializeObject(cacheData), options);
            }catch(Exception ex)
            {
                _logger.LogError($"RateLimit Exception e {ex.Message} {ex.StackTrace}");
            }
            return rateLimit;
        }
    }
}
