using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text;


namespace SeeSharpTracer.Helpers
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
            ExpirationScanFrequency = TimeSpan.FromSeconds(30)
        });

        private static readonly MemoryCache _hashCache = new MemoryCache(new MemoryCacheOptions()
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(300)
        });


        /// <summary>
        /// Retrives a path for a pid from process cache
        /// Enumeates the pid if cache entry does not exist
        /// Sets a cache entry with 30 seconds
        /// </summary>
        /// <param name="pid">Process Id</param>
        /// <returns>string</returns>
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



        /// <summary>
        /// Returns a path string, or "" if inaccessible for any reason.
        /// </summary>
        /// <param name="pid">Process Id</param>
        /// <returns>string</returns>
        public static string EnumProcessPath(int pid)
        {
            try
            {
                var processModule = Process.GetProcessById(pid).MainModule;
                // Return the file name if the main module is not null; otherwise, return an empty string.
                return processModule?.FileName ?? "";
            }
            catch
            {
                // Return an empty string if any exception occurs.
                return "";
            }
        }


        public static string ComputeMD5Hash(string rawData)
        {
            // Create a SHA256 instance
            using (MD5 md5Hash = MD5.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string
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
            // Create a SHA256 instance
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string
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
            // Create a SHA256 instance
            using (SHA512 sha256Hash = SHA512.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string
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

        /// <summary>
        /// Enumerates the process name for a given pid.
        /// </summary>
        /// <param name="pid">Process Id</param>
        /// <returns>string</returns>
        public static string EnumProcessName(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                return process.ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}