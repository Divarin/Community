using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace miniBBS.Core.Models.Control
{
    public class BbsSession : IDisposable
    {
        private bool _disposed = false;
        private ISessionsList _sessionsList;

        public BbsSession(ISessionsList sessionsList, ILogger logger)
        {
            _logger = logger;
            SessionStartUtc = DateTime.UtcNow;
            Rows = 24;
            Cols = 80;
            Id = Guid.NewGuid();
            _sessionsList = sessionsList;
            _sessionsList.AddSession(this);
            TimeZone = 0;

            Thread thread = new Thread(new ThreadStart(BeginSuicideTimer));
            thread.Start();
        }

        ~BbsSession()
        {
            Dispose();
        }

        public Action ShowPrompt { get; set; }
        public Guid Id { get; set; }
        public Stream Stream { get; set; }
        public SystemControlFlag SysControl { get; set; }
        public IRepository<User> UserRepo { get; set; }
        public IRepository<UserChannelFlag> UcFlagRepo { get; set; }
        public User User { get; set; }
        public int Rows { get; set; }
        public int Cols { get; set; }
        public int TimeZone { get; set; }
        public IUserIo Io { get; set; }
        public IMessager Messager { get; set; }
        public ISubscriber<ChannelPostMessage> ChannelPostSubscriber { get; set; }
        public ISubscriber<UserLoginOrOutMessage> UserLoginOrOutSubscriber { get; set; }
        public ISubscriber<ChannelMessage> ChannelMessageSubscriber { get; set; }
        public ISubscriber<UserMessage> UserMessageSubscriber { get; set; }
        public ISubscriber<EmoteMessage> EmoteSubscriber { get; set; }

        public Action OnDispose { get; set; }
        public bool DoNotDisturb { get; set; }
        public string IpAddress { get; set; }
        
        private DateTime _lastActivityUtc = new DateTime();
        public TimeSpan IdleTime => DateTime.UtcNow - _lastActivityUtc;

        public IDictionary<int, string> Usernames { get; set; } 
        public SortedList<int, Chat> Chats { get; set; }
        public int MsgPointer => UcFlag?.LastReadMessageNumber ?? 0;
        public int? ContextPointer { get; set; }
        public UserChannelFlag UcFlag { get; set; }
        public Channel Channel { get; set; }

        public bool ForceLogout { get; set; }

        private readonly ILogger _logger;

        public DateTime SessionStartUtc { get; private set; }

        /// <summary>
        /// Away from Keyboard flag
        /// </summary>
        public bool Afk { get; set; }
        public string AfkReason { get; set; }
        public int? LastReadMessageNumber { get; set; }

        private int _pingPongTimeMin = 0;
        private Thread _pingPongThread = null;
        public Action OnPingPong { get; set; }
        public PingPongType PingType { get; set; } = PingPongType.Full;

        /// <summary>
        /// 'on' to sound the bell on any user connecting or posting a message in the current channel
        /// otherse the username of the user to watch for, if that user connects or posts a message then sounds the bell
        /// if null or empty then no bells
        /// </summary>
        public string BellAlerts { get; set; }

        public bool NoPingPong { get; set; }

        public void StartPingPong(int delayMinutes, bool silently = true)
        {
            if (!silently)
            {
                using (Io.WithColorspace(ConsoleColor.Black, ConsoleColor.DarkGray))
                {
                    if (delayMinutes <= 0)
                        Io.OutputLine("Stopping ping pong.");
                    else
                        Io.OutputLine($"Setting ping pong every {delayMinutes} minutes.");
                }
            }

            if (_pingPongThread != null && _pingPongThread.IsAlive)
                _pingPongThread.Abort();
            _pingPongThread = null;
            _pingPongTimeMin = delayMinutes;
            ParameterizedThreadStart start = new ParameterizedThreadStart(PingPong);
            _pingPongThread = new Thread(start);
            _pingPongThread.Start(this);
        }

        private static void PingPong(object o)
        {
            BbsSession session = (BbsSession)o;
            DateTime lastPing = DateTime.Now;
            while (session._pingPongTimeMin > 0)
            {
                if ((DateTime.Now - lastPing).TotalMinutes >= session._pingPongTimeMin)
                {
                    lastPing = DateTime.Now;
                    if (!session.Io.IsInputting && !session.NoPingPong)
                    {
                        if (session.PingType == PingPongType.Full)
                        {
                            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.DarkGray))
                            {
                                session.Io.OutputLine($"{Environment.NewLine}Ping? Pong!");
                            }
                        }
                        else
                        {
                            session.Io.Output(' ');
                            session.Io.OutputBackspace();
                        }
                        session.OnPingPong?.Invoke();
                    }
                }
                else
                    Thread.Sleep(session._pingPongTimeMin * 1000);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                Messager?.Unsubscribe(ChannelPostSubscriber);
                Messager?.Unsubscribe(UserLoginOrOutSubscriber);
                Messager?.Unsubscribe(ChannelMessageSubscriber);
                Messager?.Unsubscribe(UserMessageSubscriber);
                Messager?.Unsubscribe(EmoteSubscriber);

                _sessionsList.RemoveSession(this);
            }
        }

        public void ResetIdleTimer()
        {
            _lastActivityUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// To prevent un-logged-in users from sitting idle at the login prompt this will launch a thread 
        /// that will eventually kill this session unless the user is logged on (User is not null)
        /// </summary>
        private void BeginSuicideTimer()
        {
            while (User == null)
            {
                if ((DateTime.UtcNow - SessionStartUtc).TotalMinutes > Constants.MaxLoginTimeMin)
                {
                    _logger?.Log($"{IpAddress} session terminated because user sat idle at login.", consoleOnly: true);
                    ForceLogout = true;
                    Stream.Close();
                    Dispose();
                    break;
                }
                Thread.Sleep(1000);
            }
        }

    }
}
