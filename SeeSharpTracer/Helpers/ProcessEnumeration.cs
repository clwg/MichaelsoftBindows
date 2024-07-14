using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text;


namespace EtwTracer.Helpers
{
    public class FileObject
    {
        [JsonPropertyName("path")]
        public string? Path { get; set; }
        [JsonPropertyName("hashes")]
        public Hashes Hashes { get; set; } = new Hashes();
    }

    public class Hashes
    {

        [JsonPropertyName("md5")]
        public string Md5 { get; set; } = string.Empty;
        [JsonPropertyName("sha256")]
        public string Sha256 { get; set; } = string.Empty;
        [JsonPropertyName("sha512")]
        public string Sha512 { get; set; } = string.Empty;
    }


    public class ProcessEnumeration
    {

        private static readonly MemoryCache _processCache = new MemoryCache(new MemoryCacheOptions()
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(60)
        });

        private static readonly MemoryCache _hashCache = new MemoryCache(new MemoryCacheOptions()
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(600)
        });


        public static string GetProcessPath(int pid)
        {
            if (!_processCache.TryGetValue(pid, out string? cacheEntry))
            {
                cacheEntry = EnumProcessPath(pid);
                MemoryCacheEntryOptions options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Now.AddSeconds(60)
                };
                _processCache.Set(pid, cacheEntry, options);
            }
            return cacheEntry ?? "";
        }

        public static string EnumProcessPath(int pid)
        {
            try
            {
                var processModule = Process.GetProcessById(pid).MainModule;
                return processModule?.FileName ?? "";
            }
            catch
            {
                return "";
            }
        }

        public static string ComputeMD5Hash(string rawData)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] bytes = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static string ComputeSha512Hash(string rawData)
        {
            using (SHA512 sha256Hash = SHA512.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        internal static Hashes EnumHashes(string path)
        {
            Hashes hashes = new Hashes
            {
                Md5 = ComputeMD5Hash(path),
                Sha256 = ComputeSha256Hash(path),
                Sha512 = ComputeSha512Hash(path)
            };

            return hashes;
        }
    }
}