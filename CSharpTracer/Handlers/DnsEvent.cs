using Microsoft.Diagnostics.Tracing;
using System.Text.Json.Serialization;
using CsharpTracer.Helpers;
using CSharpTracer.Logging;


namespace CsharpTracer.Handlers
{
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
        internal static void HandleDnsEvent(string eventName, TraceEvent data)
        {
            DNSData dnsObject = new DNSData
            {
                EventName = eventName,
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

            if (dnsObject.IpAddresses.Count > 0)
            {

                var logger = Logger.GetInstance();
                logger.LogEvent(eventName, data.TimeStamp.ToString(), dnsObject);

                Logging.JsonOutput.JsonSeralize(dnsObject);


                foreach (var ipAddress in dnsObject.IpAddresses)
                {
                    InternalDnsCache.AddReverseIp(ipAddress, dnsObject.QueryName);

                    var graphRecord = new Logger.GraphRecord()
                    {
                        Source = dnsObject.QueryName,
                        SourceType = "fqdn",
                        EdgeType = "DNS",
                        Target = ipAddress,
                        TargetType = "ipaddr",
                        Observations = 1,
                        FirstSeen = data.TimeStamp,
                        LastSeen = data.TimeStamp
                    };

                    logger.LogGraph(graphRecord);
                }
            }
        }
    }
}
