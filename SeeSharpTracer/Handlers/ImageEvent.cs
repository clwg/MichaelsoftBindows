using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Extensions.Caching.Memory;
using EtwTracer.Helpers;


namespace EtwTracer.Handlers
{
    internal class ImageObject
    {
        public string EventName { get; set; } = string.Empty;
        public int ProcessID { get; set; }
        public int ThreadID { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public ulong ImageBase { get; set; }
        public int ImageSize { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
        public int ImageChecksum { get; set; }
        public FileObject? ProcessFileInfo { get; set; } = new FileObject();

    }

    internal class ImageEvent
    {
        private static readonly MemoryCache _ImageCache = new MemoryCache(new MemoryCacheOptions()
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(600)
        });

        internal static void HandleImageEvent(string eventName, ImageLoadTraceData data)
        {
            var path = ProcessEnumeration.GetProcessPath(data.ProcessID);
            var processHash = ProcessEnumeration.EnumHashes(path);

            var recordKey = HashGenerator.GenerateMD5($"{processHash.Sha512}{data.EventName}{path}");

            if (!_ImageCache.TryGetValue(recordKey, out _)) {

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
                Logging.JsonOutput.JsonSeralize(imageObject);
                
                MemoryCacheEntryOptions options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Now.AddSeconds(600)
                };
                _ImageCache.Set(recordKey, true);
            }
        }
    }
}
