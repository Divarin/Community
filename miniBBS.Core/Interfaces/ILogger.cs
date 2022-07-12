using miniBBS.Core.Models.Control;

namespace miniBBS.Core.Interfaces
{
    public interface ILogger
    {
        void Log(string message, bool consoleOnly = false);
        void Log(BbsSession session, string message, bool consoleOnly = false);
        void Flush();
    }
}
