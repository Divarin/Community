using miniBBS.Core.Interfaces;
using System.IO;
using System.IO.Compression;

namespace miniBBS.Services
{
    public class Compressor : ICompressor
    {
        public byte[] Compress(byte[] decompressed)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream zipStream = new GZipStream(ms, CompressionMode.Compress))
                {
                    zipStream.Write(decompressed, 0, decompressed.Length);
                }
                return ms.ToArray();
            }
        }

        public byte[] Decompress(byte[] compressed)
        {
            const int BUFFER_SIZE = 4096;

            using (MemoryStream ms = new MemoryStream(compressed))
            {
                using (MemoryStream writeStream = new MemoryStream())
                {
                    using (GZipStream zipStream = new GZipStream(ms, CompressionMode.Decompress))
                    {
                        do
                        {
                            byte[] buffer = new byte[BUFFER_SIZE];
                            int readCount = zipStream.Read(buffer, 0, BUFFER_SIZE);
                            writeStream.Write(buffer, 0, readCount);

                            if (readCount < BUFFER_SIZE)
                                break;
                        } while (true);
                    }
                    return writeStream.ToArray();
                }
            }
        }

    }
}
