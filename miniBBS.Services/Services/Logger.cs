using miniBBS.Core;
using miniBBS.Core.Enums;
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
            _repo = GlobalDependencyResolver.Default.GetRepository<LogEntry>();
        }

        public void Flush()
        {
            lock (_lock)
            {
                while (_unwritten.TryDequeue(out LogEntry log))
                    _repo.Insert(log);
            }
        }

        public void Log(BbsSession session, string message, LoggingOptions loggingOptions = LoggingOptions.ToConsole | LoggingOptions.ToDatabase)
        {
            Log(sessionId: session?.Id,
                ipAddress: session?.IpAddress,
                userId: session?.User?.Id,
                username: session?.User?.Name,
                message: message,
                loggingOptions);
        }

        private void Log(Guid? sessionId, string ipAddress, int? userId, string username, string message, LoggingOptions loggingOptions)
        {
            var entry = new LogEntry
            {
                SessionId = sessionId,
                IpAddress = ipAddress,
                TimestampUtc = DateTime.UtcNow,
                UserId = userId,
                Message = message
            };

            if (loggingOptions.HasFlag(LoggingOptions.ToDatabase))
                _unwritten.Enqueue(entry);

            if (loggingOptions.HasFlag(LoggingOptions.ToConsole))
                SysopScreen.AddLogMessage($"{entry.TimestampUtc} : {ipAddress} : {sessionId} : {username}{Environment.NewLine}{message}{Environment.NewLine}-----");

            if (loggingOptions.HasFlag(LoggingOptions.WriteImmedately) || _unwritten.Count > Constants.NumberOfLogEntriesUntilWriteToDatabase)
                Flush();
        }
    }
}
