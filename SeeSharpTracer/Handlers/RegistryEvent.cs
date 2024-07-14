using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Extensions.Caching.Memory;
using SeeSharpTracer.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EtwTracer.Helpers.HashGenerator;


namespace EtwTracer.Handlers
{
    internal class RegistryData
    {
        public string EventName { get; set; } = string.Empty;
        public int ProcessID { get; set; }
        public int ThreadID { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string ProcessPath { get; set; } = string.Empty;
        public Hashes Hashes { get; set; } = new Hashes();
        public string KeyName { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; } = DateTime.MinValue;
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

               Logging.JsonOutput.JsonSeralize(registryData);

                MemoryCacheEntryOptions options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Now.AddSeconds(600)
                };
                _processCacheBuffer.Set(recordKey, true);
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
