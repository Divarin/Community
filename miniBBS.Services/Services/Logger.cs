using miniBBS.Core;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Services.GlobalCommands;
using System;
using System.Collections.Concurrent;

namespace miniBBS.Services.Services
{
    public class Logger : ILogger
    {
        private readonly ConcurrentQueue<LogEntry> _unwritten = new ConcurrentQueue<LogEntry>();
        private readonly IRepository<LogEntry> _repo;
        private object _lock = new object();

        public Logger()
        {
            _repo = GlobalDependencyResolver.GetRepository<LogEntry>();
        }

        public void Flush()
        {
            lock (_lock)
            {
                while (_unwritten.TryDequeue(out LogEntry log))
                    _repo.Insert(log);
            }
        }

        public void Log(BbsSession session, string message, bool consoleOnly = false)
        {
            Log(sessionId: session?.Id,
                ipAddress: session?.IpAddress,
                userId: session?.User?.Id,
                username: session?.User?.Name,
                message: message,
                consoleOnly: consoleOnly);
        }

        private void Log(Guid? sessionId, string ipAddress, int? userId, string username, string message, bool consoleOnly = false)
        {
            LogEntry entry = new LogEntry
            {
                SessionId = sessionId,
                IpAddress = ipAddress,
                TimestampUtc = DateTime.UtcNow,
                UserId = userId,
                Message = message
            };

            if (!consoleOnly)
                _unwritten.Enqueue(entry);

            SysopScreen.AddLogMessage($"{entry.TimestampUtc} : {ipAddress} : {sessionId} : {username}{Environment.NewLine}{message}{Environment.NewLine}-----");
            if (_unwritten.Count > Constants.NumberOfLogEntriesUntilWriteToDatabase)
                Flush();
        }
    }
}
