﻿using miniBBS.Core;
using miniBBS.Core.Interfaces;
using System.Linq;
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

            for (var i=0; i < hashedBytes.Length; i++)
            {
                if (Constants.Sql.IllegalCharacters.Contains((char)hashedBytes[i]))
                    hashedBytes[i] = (byte)Constants.Sql.IllegalCharacterSubstitute;
            }

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
