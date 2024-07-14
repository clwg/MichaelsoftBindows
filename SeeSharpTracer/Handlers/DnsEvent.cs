using Microsoft.Diagnostics.Tracing;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;


namespace EtwTracer.Handlers
{
    public class ReverseIpCache
    {
        private static readonly MemoryCache _reverseIpCache = new MemoryCache(new MemoryCacheOptions()
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(30)
        });


        public static HashSet<string> ReverseIpLookup(string ip)
        {
            // Try to get the cache entry for the given IP
            if (_reverseIpCache.TryGetValue(ip, out HashSet<string>? cacheEntry))
            {
                // If cacheEntry is null, return a new HashSet<string>
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
                // Rolling expiration
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1200)
            };

            var reverseIp = ReverseIpLookup(ip);
            
            // Add the hostname to the reverse lookup cache
            reverseIp.Add(hostname);

            _reverseIpCache.Set(ip, reverseIp, options);
        }
    }

        internal class DNSData
    {
        [JsonPropertyName("event_name")]
        public string EventName { get; set; } = string.Empty;

        [JsonPropertyName("process_id")]
        public int ProcessId { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("process_name")]
        public string ProcessName { get; set; } = string.Empty;

        [JsonPropertyName("thread_id")]
        public int ThreadId { get; set; }

        [JsonPropertyName("query_name")]
        public string QueryName { get; set; } = string.Empty;

        [JsonPropertyName("ip_addresses")]
        public HashSet<string> IpAddresses { get; set; } = new HashSet<string>();

        [JsonPropertyName("resource_records")]
        public List<ResourceRecord> ResourceRecords { get; set; } = new List<ResourceRecord>();

        [JsonPropertyName("process_info")]
        public FileObject? ProcessFileInfo { get; set; } = new FileObject();

    }

    internal class ResourceRecord
    {
        [JsonPropertyName("rname")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("rtype")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("rdata")]
        public string Data { get; set; } = string.Empty;
    }

    internal class DnsEvent
    {

        internal static void HandleDnsEvent(string eventType, TraceEvent data)
        {

            DNSData dnsObject = new DNSData
            {
                EventName = eventType,
                ProcessId = data.ProcessID,
                Timestamp = data.TimeStamp,
                ProcessName = data.ProcessName,
                ThreadId = data.ThreadID,
                QueryName = data.PayloadStringByName("QueryName"),
                ResourceRecords = new List<ResourceRecord>()
            };

            var path = ProcessEnumeration.GetProcessPath(data.ProcessID);
            var processHash = ProcessEnumeration.EnumHashes(path);


            dnsObject.ProcessFileInfo = new FileObject()
            {
                Path = path,
                Hashes = processHash
            };


            if (data.PayloadStringByName("QueryResults") != null)
            {
                string[] resultsSplit = data.PayloadStringByName("QueryResults").Split(';');

                var startDomain = data.PayloadStringByName("QueryName");

                foreach (string result in resultsSplit)
                {
                    if (result.Contains("type"))
                    {
                        string[] typeRecordSplit = result.Split(" ");
                        dnsObject.ResourceRecords.Add(new ResourceRecord { Name = startDomain, Type = typeRecordSplit[1], Data = typeRecordSplit[2] });
                        startDomain = typeRecordSplit[2];
                    }
                    else if (result != "")
                    {
                        dnsObject.ResourceRecords.Add(new ResourceRecord { Name = startDomain, Type = "1", Data = result }); //need to deal with ipv6
                        dnsObject.IpAddresses.Add(result);
                        
                    }
                }
            }

            // Add the IP addresses to the reverse lookup cache if the count is greater than 0
            if (dnsObject.IpAddresses.Count > 0)
            {
                foreach (var ipAddress in dnsObject.IpAddresses)
                {
                    ReverseIpCache.AddReverseIp(ipAddress, dnsObject.QueryName);
                }
            }

            if (dnsObject.ResourceRecords.Count > 0)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string jsonString = JsonSerializer.Serialize(dnsObject, options);
                Console.WriteLine(jsonString);
            }
            
        }
    }
}
