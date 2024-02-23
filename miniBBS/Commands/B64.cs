using System;
using System.Text;

namespace miniBBS.Commands
{
    public static class B64
    {
        public static string EncodeOrDecode(string str)
        {
            try
            {
                var decodedBytes = Convert.FromBase64String(str);
                var decoded = Encoding.Default.GetString(decodedBytes);
                return decoded;
            }
            catch
            {
                var encoded = Convert.ToBase64String(Encoding.Default.GetBytes(str));
                return encoded;
            }
        }
    }
}
