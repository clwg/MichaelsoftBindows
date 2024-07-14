using Microsoft.Diagnostics.Tracing.Parsers.FrameworkEventSource;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EtwTracer.Handlers
{
    internal class TCPEventData
    {
        [JsonPropertyName("event_name")]
        public string EventName { get; set; } = string.Empty;

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

        [JsonPropertyName("timestamp")]
        public DateTime TimeStamp { get; set; }

        [JsonPropertyName("dest_port")]
        public int DestPort { get; set; }

        [JsonPropertyName("process_info")]
        public FileObject? ProcessFileInfo { get; set; } = new FileObject();

    }

    internal class NetworkEvent
    { 
        internal static void HandleNetworkEvent(string eventType, TcpIpConnectTraceData data)
        {
            Console.WriteLine("Network Event");

            TCPEventData tcpEventData = new TCPEventData()
            {
                EventName = eventType,
                ProcessId = data.ProcessID,
                ProcessName = data.ProcessName,
                ThreadId = data.ThreadID,
                TimeStamp = data.TimeStamp,
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

            var dnsCacheQueries = ReverseIpCache.ReverseIpLookup(destAddr);
            tcpEventData.DNSCacheQueries = dnsCacheQueries;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string jsonString = JsonSerializer.Serialize(tcpEventData, options);
            Console.WriteLine(jsonString);

            Console.WriteLine("Network Event Finished");

        }
    }
}
