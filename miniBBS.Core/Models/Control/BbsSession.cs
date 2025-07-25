﻿using miniBBS.Core.Enums;
using miniBBS.Core.Extensions;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace miniBBS.Core.Models.Control
{
    public class BbsSession : IDisposable
    {
        private bool _disposed = false;
        private readonly ISessionsList _sessionsList;
        private static readonly Random _random = new Random((int)DateTime.Now.Ticks % int.MaxValue);
        private static readonly string _on = $"{Constants.InlineColorizer}{(int)ConsoleColor.Red}{Constants.InlineColorizer}On{Constants.InlineColorizer}-1{Constants.InlineColorizer}";        
        private static readonly string _off = $"{Constants.InlineColorizer}{(int)ConsoleColor.Green}{Constants.InlineColorizer}Off{Constants.InlineColorizer}-1{Constants.InlineColorizer}";

        public BbsSession(ISessionsList sessionsList, Stream stream, TcpClient client)
        {
            _stream = stream;
            _client = client;

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

        public void Disconnect()
        {
            try
            {
                Thread.Sleep(250);
                this._stream?.Close();
                this._client?.Close();
            }
            catch
            {

            }
            finally
            {
                this.Dispose();
            }
        }

        public string PreviousFilesDirectory { get; set; }

        ~BbsSession()
        {
            Dispose();
        }

        public Action ShowPrompt { get; set; }
        public Guid Id { get; set; }

        private Stream _stream;
        private readonly TcpClient _client;

        public Stream Stream => _stream;
        public bool IsDisposed => _disposed;

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
        public ISubscriber<NewUserRegisteredMessage> NewUserRegisteredSubscriber { get; set; }

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

                if (!_doNotDisturb && DndMessages.Count > 0 &&
                    (!Items.ContainsKey(SessionItem.DoNotShowDndSummary) || (bool)Items[SessionItem.DoNotShowDndSummary] != true))
                {
                    ShowDndMessages();
                }

                if (changed && User != null && Channel != null)
                {
                    Io.OutputLine(string.Format("{0}You are {1} in Do Not Disturb (DND) mode.{2}", 
                        $"{Constants.InlineColorizer}{(int)ConsoleColor.Red}{Constants.InlineColorizer}",
                        _doNotDisturb ? "now" : "no longer",
                        $"{Constants.InlineColorizer}-1{Constants.InlineColorizer}"));

                    var location = CurrentLocation == Module.Chat ? $"Chat ({Channel.Name})" : CurrentLocation.FriendlyName();

                    var channelMessage = new ChannelMessage(
                        Id,
                        Channel.Id,
                        $"{User.Name}: DND {(_doNotDisturb ? _on : _off)} @ {location}.",
                        predicate: x => !x.DoNotDisturb);

                    Messager.Publish(this, channelMessage);
                }
            }
        }

        public string IpAddress { get; set; }
        
        private DateTime _lastActivityUtc = new DateTime();
        public TimeSpan IdleTime => DateTime.UtcNow - _lastActivityUtc;
        public IDictionary<int, string> Usernames { get; set; } 
        
        /// <summary>
        /// [ID] = Chat
        /// </summary>
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

        private bool _forceLogout = false;
        public bool ForceLogout => _forceLogout;

        public DateTime SessionStartUtc { get; private set; }

        /// <summary>
        /// Away from Keyboard flag
        /// </summary>
        public bool Afk { get; set; }
        public string AfkReason { get; set; }
        public SessionControlFlags ControlFlags { get; set; } = SessionControlFlags.None;

        private double _pingPongTimeMin = 0;
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

        public void SetForcedLogout(string reason)
        {
            ForceLogoutReason = reason;
            _forceLogout = true;
        }

        public void StartPingPong(double delayMinutes, bool silently = true)
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

        public IDictionary<SessionItem, object> Items { get; } = new Dictionary<SessionItem, object>();
        
        /// <summary>
        /// When in replay screensaver mode, this is the next message number to replay
        /// </summary>
        public int ReplayNum { get; set; }
        public bool IsConnected
        {
            get
            {
                var networkSteram = Stream as NetworkStream;
                var socketProperty = typeof(NetworkStream)
                    .GetProperty("Socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var socket = (Socket)socketProperty.GetValue(networkSteram, null);
                return socket.Connected;
            }
        }

        public string ForceLogoutReason { get; set; }
        
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
                        switch (session.PingType)
                        {
                            case PingPongType.Full:
                                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.DarkGray))
                                    session.Io.OutputLine($"{Environment.NewLine}Ping? Pong!");
                                break;
                            case PingPongType.Invisible:
                                session.Io.Output(' ');
                                session.Io.OutputBackspace();
                                break;
                            case PingPongType.ScreenSaver:
                                session.Io.Output(Repeat(Environment.NewLine, _random.Next(3, 8)));
                                session.Io.Output(Repeat(" ", _random.Next(5, session.Cols-31)));
                                session.Io.Output($"[{Constants.BbsName} ({DateTime.UtcNow.AddHours(session.TimeZone):HH:mm:ss})]");
                                session.Io.Output(Repeat(Environment.NewLine, _random.Next(3, 8)));
                                session.Io.Output(Repeat(" ", _random.Next(5, session.Cols - 1)));
                                break;
                            case PingPongType.Replay:
                                // session.OnPingPong should do this as it can't be implemented in Core
                                break;
                        }
                        session.OnPingPong?.Invoke();
                    }
                }
                else
                {
                    var delay = (int)(session._pingPongTimeMin * 1000);
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

                _sessionsList?.RemoveSession(this);
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
                try
                {
                    string reason = null;
                    if (ForceLogout)
                        reason = $"Force Logout flag {ForceLogoutReason}";
                    else if (Stream == null)
                        reason = "Stream is null";
                    else if (!Stream.CanRead)
                        reason = "Can't read from Stream";
                    else if (!Stream.CanWrite)
                        reason = "Can't write to Stream";
                    else if (User == null && (DateTime.UtcNow - SessionStartUtc).TotalMinutes > Constants.MaxLoginTimeMin)
                        reason = "User took too long to log in";

                    bool shouldHangUp = reason != null;

                    if (shouldHangUp)
                    {
                        if (!ForceLogout)
                        {
                            SetForcedLogout(reason);
                        }
                        Stream?.Close();
                        Dispose();
                        break;
                    }
                    Thread.Sleep(threadSleepTimeMs);
                }
                catch (Exception ex)
                {
                    SetForcedLogout($"Exception happened in suicide timer: {ex.Message}");
                    Stream?.Close();
                    Dispose();
                    break;
                }
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

        private static string Repeat(string str, int count)
        {
            var arr = new string[count];
            for (int i = 0; i < count; i++)
                arr[i] = str;
            var repeated = string.Join("", arr);
            return repeated;
        }

    }
}
