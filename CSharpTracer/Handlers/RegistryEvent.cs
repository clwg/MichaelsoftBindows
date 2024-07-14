using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using CsharpTracer.Helpers;
using static CsharpTracer.Helpers.HashGenerator;
using CSharpTracer.Logging;

namespace CsharpTracer.Handlers
{
    internal class RegistryData
    {
        [JsonPropertyName("event_name")]
        public string EventName { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime TimeStamp { get; set; }

        [JsonPropertyName("process_id")]
        public int ProcessID { get; set; }

        [JsonPropertyName("thread_id")]
        public int ThreadID { get; set; }

        [JsonPropertyName("process_name")]
        public string ProcessName { get; set; } = string.Empty;

        [JsonPropertyName("process_path")]
        public string ProcessPath { get; set; } = string.Empty;

        [JsonPropertyName("hashes")]
        public Hashes Hashes { get; set; } = new Hashes();

        [JsonPropertyName("key_name")]
        public string KeyName { get; set; } = string.Empty;
    }

    internal class RegistryEvent
    {

        private static readonly MemoryCache _processCacheBuffer = new MemoryCache(new MemoryCacheOptions()
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(600)
        });

        internal static void HandleRegistryEvent(string eventType, RegistryTraceData data)
        {
            var path = ProcessEnumeration.GetProcessPath(data.ProcessID);
            var processHash = ProcessEnumeration.EnumHashes(path);
            var processName = data.ProcessName;
            var timestamp = data.TimeStamp;
            var keyName = ReplaceGuidInKeyName(data.KeyName);

            var recordKey = GenerateMD5($"{processHash.Sha512}{data.EventName}{keyName}{path}");

            if (!_processCacheBuffer.TryGetValue(recordKey, out _))
            {
                var registryData = new RegistryData
                {
                    EventName = data.EventName,
                    ProcessID = data.ProcessID,
                    ThreadID = data.ThreadID,
                    ProcessName = processName,
                    ProcessPath = path,
                    Hashes = processHash,
                    KeyName = keyName,
                    TimeStamp = timestamp
                };

                MemoryCacheEntryOptions options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Now.AddSeconds(600)
                };
                _processCacheBuffer.Set(recordKey, true);

                Logging.JsonOutput.JsonSeralize(registryData);
                var logger = Logger.GetInstance("logs.db");
                logger.LogEvent(data.EventName, data.TimeStamp.ToString(), registryData);
            }
            else
            {
                return;
            }
        }

        private static string ReplaceGuidInKeyName(string keyName)
        {
            return Regex.Replace(keyName, @"\{[0-9A-Fa-f\-]{36}\}", "{guid}");
        }
    }
}
