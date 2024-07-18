using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using System.Text.Json.Serialization;
using CsharpTracer.Helpers;
using CSharpTracer.Logging;

namespace CsharpTracer.Handlers
{
    internal class TCPEventData
    {
        [JsonPropertyName("event_name")]
        public string EventName { get; set; } = string.Empty;
        
        [JsonPropertyName("timestamp")]
        public DateTime TimeStamp { get; set; }

        [JsonPropertyName("dst_addr")]
        public string DestAddr { get; set; } = string.Empty; //casting as a string is intentional for generalized matching

        [JsonPropertyName("dns_queries")]
        public HashSet<string> DNSCacheQueries { get; set; } = new HashSet<string>();

        [JsonPropertyName("process_id")]
        public int ProcessId { get; set; }

        [JsonPropertyName("process_name")]
        public string ProcessName { get; set; } = string.Empty;

        [JsonPropertyName("thread_id")]
        public int ThreadId { get; set; }

        [JsonPropertyName("dest_port")]
        public int DestPort { get; set; }

        [JsonPropertyName("process_info")]
        public FileObject? ProcessFileInfo { get; set; } = new FileObject();

    }

    internal class NetworkEvent
    { 
        internal static void HandleNetworkEvent(string eventName, TcpIpConnectTraceData data)
        {
            TCPEventData tcpEventData = new TCPEventData()
            {
                EventName = eventName,
                TimeStamp = data.TimeStamp,
                ProcessId = data.ProcessID,
                ProcessName = data.ProcessName,
                ThreadId = data.ThreadID,
                DestPort = data.dport
            };

            var path = ProcessEnumeration.GetProcessPath(data.ProcessID);
            var processHash = ProcessEnumeration.EnumHashes(path);

            tcpEventData.ProcessFileInfo = new FileObject()
            {
                Path = path,
                Hashes = processHash
            };

            var destAddr = data.daddr.ToString(); 
            tcpEventData.DestAddr = destAddr;

            var dnsCacheQueries = InternalDnsCache.ReverseIpLookup(destAddr);
            tcpEventData.DNSCacheQueries = dnsCacheQueries;

            Logging.JsonOutput.JsonSeralize(tcpEventData);
            var logger = Logger.GetInstance();
            logger.LogEvent(eventName, data.TimeStamp.ToString(), tcpEventData);
        }
    }
}
