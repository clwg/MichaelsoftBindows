using System;
using System.Security.Cryptography;
using System.Text;

namespace CsharpTracer.Helpers
{
    internal class HashGenerator
    {
        public static string GenerateSHA1(string input)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha1.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static string GenerateMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private static readonly Guid NamespaceUUID = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        public static Guid GenerateUUIDv5(string input)
        {
            var namespaceBytes = NamespaceUUID.ToByteArray();
            SwapByteOrder(namespaceBytes);

            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashInput = new byte[namespaceBytes.Length + inputBytes.Length];
            Buffer.BlockCopy(namespaceBytes, 0, hashInput, 0, namespaceBytes.Length);
            Buffer.BlockCopy(inputBytes, 0, hashInput, namespaceBytes.Length, inputBytes.Length);

            using (SHA1 sha1 = SHA1.Create())
            {
                var hash = sha1.ComputeHash(hashInput);
                hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
                hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

                var newGuidBytes = new byte[16];
                Array.Copy(hash, 0, newGuidBytes, 0, 16);
                SwapByteOrder(newGuidBytes);

                return new Guid(newGuidBytes);
            }
        }

        private static void SwapByteOrder(byte[] guidBytes)
        {
            SwapBytes(guidBytes, 0, 3);
            SwapBytes(guidBytes, 1, 2);
            SwapBytes(guidBytes, 4, 5);
            SwapBytes(guidBytes, 6, 7);
        }

        private static void SwapBytes(byte[] guidBytes, int left, int right)
        {
            byte temp = guidBytes[left];
            guidBytes[left] = guidBytes[right];
            guidBytes[right] = temp;
        }
    }
}
