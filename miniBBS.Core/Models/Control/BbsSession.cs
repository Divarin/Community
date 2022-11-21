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
        private readonly ISessionsList _sessionsList;

        public BbsSession(ISessionsList sessionsList)
        {
            SessionStartUtc = DateTime.UtcNow;
            Rows = 24;
            Cols = 80;
            Id = Guid.NewGuid();
            _sessionsList = sessionsList;
            _sessionsList.AddSession(this);
            TimeZone = 0;

            ResetIdleTimer();

            Thread thread = new Thread(new ThreadStart(BeginSuicideTimer));
            thread.Start();
        }

        public string PreviousFilesDirectory { get; set; }

        ~BbsSession()
        {
            Dispose();
        }

        public Action ShowPrompt { get; set; }
        public Guid Id { get; set; }
        public Stream Stream { get; set; }
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
        public ISubscriber<GlobalMessage> GlobalMessageSubscriber { get; set; }
        public ISubscriber<UserMessage> UserMessageSubscriber { get; set; }
        public ISubscriber<EmoteMessage> EmoteSubscriber { get; set; }
        public Module CurrentLocation { get; set; }
        public Action OnDispose { get; set; }

        private bool _doNotDisturb = false;
        public bool DoNotDisturb
        {
            get
            {
                return _doNotDisturb;
            }
            set
            {
                var changed = _doNotDisturb != value;
                _doNotDisturb = value;

                if (!_doNotDisturb && DndMessages.Count > 0)
                    ShowDndMessages();

                if (changed && User != null && Channel != null)
                    Messager.Publish(this, new ChannelMessage(Id, Channel.Id, $"{User.Name} is {(_doNotDisturb ? "now" : "no longer")} in Do Not Disturb (DND) mode."));
            }
        }

        public string IpAddress { get; set; }
        
        private DateTime _lastActivityUtc = new DateTime();
        public TimeSpan IdleTime => DateTime.UtcNow - _lastActivityUtc;
        public IDictionary<int, string> Usernames { get; set; } 
        public SortedList<int, Chat> Chats { get; set; }

        #region message pointers
        /// <summary>
        /// The next message the user will read when they hit enter
        /// </summary>
        public int MsgPointer { get; set; } // => UcFlag?.LastReadMessageNumber ?? 0;
        /// <summary>
        /// The last message the user read when they hit enter (not including new, live, messages)
        /// </summary>
        public int? LastMsgPointer { get; set; }
        /// <summary>
        /// The last message the user read (including new, live, messages)
        /// </summary>
        public int? LastReadMessageNumber { get; set; }
        /// <summary>
        /// The last message the user read (including new, live, messages) when they began typing a response 
        /// If a new message shows up (updating LastReadMessageNumber) while they are typing a response then 
        /// this value is unaffected.  This sets the re: number for that response
        /// </summary>
        public int? LastReadMessageNumberWhenStartedTyping { get; set; }
        /// <summary>
        /// The last message the user read using /re or /ra, used to find the next re: or ra message or to 
        /// extract textfiles/basic program links from the message.
        /// </summary>
        public int? ContextPointer { get; set; }
        #endregion

        public UserChannelFlag UcFlag { get; set; }
        public Channel Channel { get; set; }
        public bool ForceLogout { get; set; }
        public DateTime SessionStartUtc { get; private set; }

        /// <summary>
        /// Away from Keyboard flag
        /// </summary>
        public bool Afk { get; set; }
        public string AfkReason { get; set; }
        public SessionControlFlags ControlFlags { get; set; } = SessionControlFlags.None;

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
        
        public Queue<Action> DndMessages { get; } = new Queue<Action>();

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

        public string LastLine { get; set; }

        public IDictionary<SessionItem, object> Items { get; } = new Dictionary<SessionItem, object>();

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
                {
                    var delay = session._pingPongTimeMin * 1000;
                    if (delay > 0 && delay < int.MaxValue)
                        Thread.Sleep(delay);
                }
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
                Messager?.Unsubscribe(GlobalMessageSubscriber);

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
            const int threadSleepTimeMs = 60 * Constants.MaxLoginTimeMin * 1000;

            while (true)
            {
                bool shouldHangUp = (DateTime.UtcNow - SessionStartUtc).TotalMinutes > Constants.MaxLoginTimeMin;
                shouldHangUp &= User == null || !Stream.CanRead || !Stream.CanWrite;

                if (shouldHangUp)
                {
                    ForceLogout = true;
                    Stream.Close();
                    Dispose();
                    break;
                }
                Thread.Sleep(threadSleepTimeMs);
            }
        }

        private void ShowDndMessages()
        {
            char? key;
            do
            {
                using (Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    Io.OutputLine($"While you where in 'Do Not Disturb' mode, {DndMessages.Count} things happened.");
                    Io.OutputLine("What do you want to do about it?");
                    Io.SetForeground(ConsoleColor.Yellow);
                    Io.OutputLine("R) Read all the things that happened.");
                    Io.OutputLine("Q) Quit and forget about all those things.");
                    Io.SetForeground(ConsoleColor.Cyan);
                    Io.Output("What now? (R, Q): ");
                    key = Io.InputKey();
                    Io.OutputLine();
                }

                switch (key)
                {
                    case 'R':
                    case 'r':
                        while (DndMessages.Count > 0)
                            DndMessages.Dequeue().Invoke();
                        return;
                    case 'Q':
                    case 'q':
                        DndMessages.Clear();
                        return;
                }
            } while (true);
        }

    }
}
