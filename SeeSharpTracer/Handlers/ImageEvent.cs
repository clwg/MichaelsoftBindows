using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Extensions.Caching.Memory;
using EtwTracer.Helpers;
using System.Text.Json.Serialization;


namespace EtwTracer.Handlers
{
    internal class ImageObject
    {
        [JsonPropertyName("eventname")]
        public string EventName { get; set; } = string.Empty;
        
        [JsonPropertyName("timestamp")]
        public DateTime TimeStamp { get; set; }

        [JsonPropertyName("processid")]
        public int ProcessID { get; set; }

        [JsonPropertyName("threadid")]
        public int ThreadID { get; set; }

        [JsonPropertyName("processname")]
        public string ProcessName { get; set; } = string.Empty;

        [JsonPropertyName("imagebase")]
        public ulong ImageBase { get; set; }

        [JsonPropertyName("imagesize")]
        public int ImageSize { get; set; }

        [JsonPropertyName("filename")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("imagechecksum")]
        public int ImageChecksum { get; set; }

        [JsonPropertyName("processfileinfo")]
        public FileObject? ProcessFileInfo { get; set; } = new FileObject();
    }

    internal class ImageEvent
    {
        private static readonly MemoryCache _imageCache = new MemoryCache(new MemoryCacheOptions()
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(600)
        });

        internal static void HandleImageEvent(string eventName, ImageLoadTraceData data)
        {
            var path = ProcessEnumeration.GetProcessPath(data.ProcessID);
            var processHash = ProcessEnumeration.EnumHashes(path);

            var recordKey = HashGenerator.GenerateMD5($"{processHash.Sha512}{data.EventName}{path}");

            if (!_imageCache.TryGetValue(recordKey, out _)) {

                ImageObject imageObject = new ImageObject()
                {
                    EventName = eventName,
                    ProcessID = data.ProcessID,
                    ThreadID = data.ThreadID,
                    ProcessName = data.ProcessName,
                    ImageBase = data.ImageBase,
                    ImageSize = data.ImageSize,
                    FileName = data.FileName,
                    TimeStamp = data.TimeStamp,
                    ImageChecksum = data.ImageChecksum,
                    ProcessFileInfo = new FileObject()
                    {
                        Path = path,
                        Hashes = processHash
                    },
                };
        
                MemoryCacheEntryOptions options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Now.AddSeconds(600)
                };
                _imageCache.Set(recordKey, true);

                Logging.JsonOutput.JsonSeralize(imageObject);
            }
        }
    }
}
