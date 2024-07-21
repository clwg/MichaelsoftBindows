using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Serialization;
using CsharpTracer.Helpers;
using CSharpTracer.Logging;


namespace CsharpTracer.Handlers
{
    internal class ThreadObject
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

        [JsonPropertyName("parent_process_id")]
        public int ParentProcessId { get; set; }

        [JsonPropertyName("process_file_info")]
        public FileObject ProcessFileInfo { get; set; } = new FileObject();

        [JsonPropertyName("parent_process")]
        public ParentProcessinfo ParentProcess { get; set; } = new ParentProcessinfo();
    }

    internal class ThreadEvent
    {
        private static readonly MemoryCache _threadCache = new MemoryCache(new MemoryCacheOptions()
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(600)
        });

        internal static void HandleThreadEvent(string eventName, ThreadTraceData data)
        {
            var path = ProcessEnumeration.GetProcessPath(data.ProcessID);
            var processHash = ProcessEnumeration.EnumHashes(path);
            var parentPath = ProcessEnumeration.GetProcessPath(data.ParentProcessID);
            var parentHash = ProcessEnumeration.EnumHashes(parentPath);

            var recordKey = HashGenerator.GenerateMD5($"{processHash.Sha512}{parentHash.Sha512}{data.EventName}{path}");

            if (!_threadCache.TryGetValue(recordKey, out _))
            {
                ThreadObject threadObject = new ThreadObject()
                {
                    EventName = eventName,
                    ProcessId = data.ProcessID,
                    Timestamp = data.TimeStamp,
                    ProcessName = data.ProcessName,
                    ThreadId = data.ThreadID,
                    ParentProcessId = data.ParentProcessID,
                    ProcessFileInfo = new FileObject()
                    {
                        Path = path,
                        Hashes = processHash
                    },
                    ParentProcess = new ParentProcessinfo()
                    {
                        ProcessId = data.ParentProcessID,
                        ProcessFileInfo = new FileObject()
                        {
                            Path = parentPath,
                            Hashes = parentHash
                        }
                    }
                };

                MemoryCacheEntryOptions options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Now.AddSeconds(600)
                };
                _threadCache.Set(recordKey, true);

                Logging.JsonOutput.JsonSeralize(threadObject);
                var logger = Logger.GetInstance();
                logger.LogEvent(eventName, data.TimeStamp.ToString(), threadObject);

                var graphRecord = new Logger.GraphRecord()
                {
                    Source = processHash.Sha256,
                    SourceType = "sha256",
                    Target = parentHash.Sha256,
                    TargetType = "sha256",
                    EdgeType = data.EventName,
                    Observations = 1,
                    FirstSeen = data.TimeStamp,
                    LastSeen = data.TimeStamp
                };

                logger.LogGraph(graphRecord);
            }
        }
    }
}
