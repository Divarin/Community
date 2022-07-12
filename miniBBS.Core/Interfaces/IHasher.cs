namespace miniBBS.Core.Interfaces
{
    public interface IHasher
    {
        string Hash(string unhashed);
        bool VerifyHash(string unhashed, string hashed);
    }
}
