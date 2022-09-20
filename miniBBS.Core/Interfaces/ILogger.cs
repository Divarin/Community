using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;

namespace miniBBS.Core.Interfaces
{
    public interface ILogger
    {
        void Log(BbsSession session, string message, LoggingOptions loggingOptions = LoggingOptions.ToConsole | LoggingOptions.ToDatabase);
        void Flush();
    }
}
