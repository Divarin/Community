using miniBBS.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace miniBBS.Helpers
{
    public class Hasher : IHasher
    {
        private SHA256 _sha;
        public Hasher()
        {
            _sha = SHA256.Create();
        }

        public string Hash(string unhashed)
        {
            byte[] unhashedBytes = Encoding.UTF8.GetBytes(unhashed);
            byte[] hashedBytes = _sha.ComputeHash(unhashedBytes);
            string result = Encoding.UTF8.GetString(hashedBytes);
            return result;
        }

        public bool VerifyHash(string unhashed, string hashed)
        {
            return 
                unhashed != null &&
                hashed != null &&
                Hash(unhashed).Equals(hashed);
        }
    }
}
