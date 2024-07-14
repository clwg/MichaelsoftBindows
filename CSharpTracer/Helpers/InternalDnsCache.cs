using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpTracer.Helpers
{
    internal class InternalDnsCache
    {
        private static readonly MemoryCache _reverseIpCache = new MemoryCache(new MemoryCacheOptions()
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(30)
        });

        public static HashSet<string> ReverseIpLookup(string ip)
        {
            if (_reverseIpCache.TryGetValue(ip, out HashSet<string>? cacheEntry))
            {
                return cacheEntry ?? new HashSet<string>();
            }
            else
            {
                return new HashSet<string>();
            }
        }

        public static void AddReverseIp(string ip, string hostname)
        {
            MemoryCacheEntryOptions options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1200)
            };

            var reverseIp = InternalDnsCache.ReverseIpLookup(ip);
            reverseIp.Add(hostname);

            _reverseIpCache.Set(ip, reverseIp, options);
        }
    }
}

