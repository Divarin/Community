namespace miniBBS.Core.Interfaces
{
    public interface ICompressor
    {
        byte[] Compress(byte[] uncompressed);
        byte[] Decompress(byte[] compressed);
    }
}
