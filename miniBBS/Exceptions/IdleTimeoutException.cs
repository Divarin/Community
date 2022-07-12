using System;

namespace miniBBS.Exceptions
{
    public class IdleTimeoutException : Exception
    {
        public IdleTimeoutException() : base()
        {

        }

        public IdleTimeoutException(string message) : base(message)
        {

        }
    }
}
