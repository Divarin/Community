using miniBBS.Basic.Models;
using System;

namespace miniBBS.Basic.Exceptions
{
    public class RuntimeException : Exception
    {
        public RuntimeException()
        {
        }

        public RuntimeException(string message)
            : base(message)
        {
        }

        public StatementPointer ExceptionLocation { get; set; }
    }
}
