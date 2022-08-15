using System;

namespace miniBBS.Basic.Exceptions
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
