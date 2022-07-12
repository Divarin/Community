using System;

namespace miniBBS.Extensions
{
    public static class ExceptionExtensions
    {
        public static Exception InnermostException(this Exception ex)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;
            return ex;
        }
    }
}
