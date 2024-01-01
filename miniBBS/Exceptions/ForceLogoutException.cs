using System;

namespace miniBBS.Exceptions
{
    public class ForceLogoutException : Exception
    {
        public ForceLogoutException() : base()
        {

        }

        public ForceLogoutException(string message) : base(message)
        {

        }
    }
}
