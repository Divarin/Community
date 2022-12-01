using System;
using System.Collections.Generic;

namespace miniBBS.Extensions_Exception
{
    public static class ExceptionExtensions
    {
        public static Exception InnermostException(this Exception ex)
        {
            while (ex?.InnerException != null)
                ex = ex.InnerException;
            return ex;
        }

        public static IEnumerable<Exception> AllExceptions(this Exception ex)
        {
            while (ex != null)
            {
                yield return ex;
                ex = ex.InnerException;
            }
        }
    }
}
