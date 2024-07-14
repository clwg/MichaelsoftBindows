using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using EtwTracer.Helpers;

namespace EtwTracer.Handlers
{
    internal class ThreadObject
    {
        public string EventName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public DateTime Timestamp { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public int ThreadId { get; set; }
        public int ParentProcessId { get; set; }
        public FileObject processFileInfo { get; set; } = new FileObject();
        public ParentProcessinfo parentProcess { get; set; } = new ParentProcessinfo();
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
                    processFileInfo = new FileObject()
                    {
                        Path = path,
                        Hashes = processHash
                    },
                    parentProcess = new ParentProcessinfo()
                    {
                        ProcessId = data.ParentProcessID,
                        processFileInfo = new FileObject()
                        {
                            Path = parentPath,
                            Hashes = parentHash
                        }
                    }
                };

                Logging.JsonOutput.JsonSeralize(threadObject);

                MemoryCacheEntryOptions options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Now.AddSeconds(600)
                };
                _threadCache.Set(recordKey, true);
            }            
        }
    }
}
