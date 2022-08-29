using miniBBS.Core.Interfaces;
using System;
using System.Text;

namespace miniBBS.Extensions
{
    public static class CompressorExtensions
    {
        public static string Compress(this ICompressor compressor, string uncompressed)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(uncompressed);
            byte[] compressed = compressor.Compress(bytes);
            string result = Convert.ToBase64String(compressed);
            return result;
        }

        public static string Decompress(this ICompressor compressor, string compressed)
        {
            byte[] bytes = Convert.FromBase64String(compressed);
            byte[] decompressed = compressor.Decompress(bytes);
            string result = Encoding.UTF8.GetString(decompressed);
            return result;
        }
    }
}
