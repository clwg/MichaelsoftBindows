using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using System.Text.Json.Serialization;
using CsharpTracer.Helpers;
using CSharpTracer.Logging;


namespace CsharpTracer.Handlers
{
    internal class ProcessObject
    {
        [JsonPropertyName("image_filename")]
        public string ImageFileName { get; set; } = string.Empty;

        [JsonPropertyName("command_line")]
        public string CommandLine { get; set; } = string.Empty;

        [JsonPropertyName("event_name")]
        public string EventName { get; set; } = string.Empty;

        [JsonPropertyName("kernel_image_filename")]
        public string KernelImageFileName { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("unique_processkey")]
        public ulong UniqueProcessKey { get; set; }

        [JsonPropertyName("process_id")]
        public int ProcessId { get; set; }

        [JsonPropertyName("process_file_info")]
        public FileObject? ProcessFileInfo { get; set; } = new FileObject();

        [JsonPropertyName("parent_process_info")]
        public ParentProcessinfo? ParentProcessInfo { get; set; } = new ParentProcessinfo();
    }

    internal class ParentProcessinfo
    {
        [JsonPropertyName("process_id")]
        public int ProcessId { get; set; }

        [JsonPropertyName("process_file_info")]
        public FileObject ProcessFileInfo { get; set; } = new FileObject();
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
                    ProcessFileInfo = new FileObject()
                    {
                        Path = parentPath,
                        Hashes = parentHash
                    }
                }
            };

            Logging.JsonOutput.JsonSeralize(processObject);
            var logger = Logger.GetInstance();
            logger.LogEvent(eventName, data.TimeStamp.ToString(), processObject);

            var graphRecord = new Logger.GraphRecord()
            {
                Source = processHash.Sha256,
                SourceType = "sha256",
                EdgeType = data.EventName,
                Target = parentHash.Sha256,
                TargetType = "sha256",
                Observations = 1,
                FirstSeen = data.TimeStamp,
                LastSeen = data.TimeStamp
            };

            logger.LogGraph(graphRecord);

            var graphPathRecord = new Logger.GraphRecord()
            {
                Source = processHash.Sha256,
                SourceType = "sha256",
                EdgeType = "filepath",
                Target = path,
                TargetType = "filepath",
                Observations = 1,
                FirstSeen = data.TimeStamp,
                LastSeen = data.TimeStamp
            };

            logger.LogGraph(graphPathRecord);

            var parentGraphPathRecord = new Logger.GraphRecord()
            {
                Source = parentHash.Sha256,
                SourceType = "sha256",
                EdgeType = "filepath",
                Target = parentPath,
                TargetType = "filepath",
                Observations = 1,
                FirstSeen = data.TimeStamp,
                LastSeen = data.TimeStamp
            };

            logger.LogGraph(graphPathRecord);
        }
    }
}
