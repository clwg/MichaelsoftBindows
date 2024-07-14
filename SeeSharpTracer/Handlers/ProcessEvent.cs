using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtwTracer.Handlers
{
    internal class ProcessObject
    {
        public string ImageFileName { get; set; } = string.Empty;
        public string CommandLine { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string KernelImageFileName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public ulong UniqueProcessKey { get; set; }
        public int ProcessId { get; set; }
        public FileObject? ProcessFileInfo { get; set; } = new FileObject();
        public ParentProcessinfo? ParentProcessInfo { get; set; } = new ParentProcessinfo();
    }

    internal class ParentProcessinfo
    {
        public int ProcessId { get; set; }
        public FileObject processFileInfo { get; set; } = new FileObject();
    }


    internal class ProcessEvent
    {
        internal static void HandleProcessEvent(string eventName, ProcessTraceData data)
        {
            var path = ProcessEnumeration.GetProcessPath(data.ProcessID);
            var processHash = ProcessEnumeration.EnumHashes(path);
            var parentPath = ProcessEnumeration.GetProcessPath(data.ParentID);
            var parentHash = ProcessEnumeration.EnumHashes(parentPath);

            ProcessObject processObject = new ProcessObject()
            {
                ImageFileName = data.ImageFileName,
                CommandLine = data.CommandLine,
                EventName = eventName,
                KernelImageFileName = data.KernelImageFileName,
                Timestamp = data.TimeStamp,
                UniqueProcessKey = data.UniqueProcessKey,
                ProcessId = data.ProcessID,
                ProcessFileInfo = new FileObject()
                {
                    Path = path,
                    Hashes = processHash
                },
                ParentProcessInfo = new ParentProcessinfo()
                {
                    ProcessId = data.ParentID,
                    processFileInfo = new FileObject()
                    {
                        Path = parentPath,
                        Hashes = parentHash
                    }
                }
            };
            Logging.JsonOutput.JsonSeralize(processObject);
        }
    }
}
