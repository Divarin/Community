using miniBBS.Commands;
using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using miniBBS.Helpers;
using miniBBS.Menus;
using miniBBS.Persistence;
using miniBBS.Subscribers;
using miniBBS.UserIo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace miniBBS
{
    class Program
    {
        private static ILogger _logger;
        private static List<string> _ipBans;

        static void Main(string[] args)
        {
            if (args?.Length < 1 || !int.TryParse(args[0], out int port))
                port = 23;

            if (args?.Length >= 2 && args[1] == "local")
                Constants.IsLocal = true;

            _logger = DI.Get<ILogger>();

            new DatabaseInitializer().InitializeDatabase();

            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            SystemControlFlag sysControl = SystemControlFlag.Normal;
            var sessionsList = DI.Get<ISessionsList>();
            Console.WriteLine(Constants.Version);
            _ipBans = DI.GetRepository<IpBan>().Get()
                .Select(x => x.IpMask)
                .ToList();

            SysopScreen.Initialize(sessionsList);

            while (!sysControl.HasFlag(SystemControlFlag.Shutdown))
            {
                try
                {
                    TcpClientFactory clientFactory = new TcpClientFactory(listener);
                    clientFactory.AwaitConnection();
                    while (clientFactory.Client == null)
                    {
                        Thread.Sleep(25);
                    }
                    TcpClient client = clientFactory.Client;
                    ParameterizedThreadStart threadStart = new ParameterizedThreadStart(BeginConnection);
                    Thread thread = new Thread(threadStart);
                    thread.Start(new NodeParams
                    {
                        Client = client,
                        SysControl = sysControl,
                        Messager = DI.Get<IMessager>()
                    });
                } 
                catch (Exception ex)
                {
                    _logger.Log($"{DateTime.Now} - {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                }
            }

            listener.Stop();            
        }

        private static void BeginConnection(object o)
        {
            BbsSession session = null;
            NodeParams nodeParams = (NodeParams)o;

            try
            {
                var sessionsList = DI.Get<ISessionsList>();

                var client = nodeParams.Client;
                var sysControl = nodeParams.SysControl;

                var ip = (client.Client.RemoteEndPoint as IPEndPoint)?.Address?.ToString();
                if (true == _ipBans?.Any(x => FitsMask(ip, x)))
                {
                    Console.WriteLine($"Banned ip {ip} rejected.");
                    return;
                }

                //_logger.Log($"{ip} connected.", consoleOnly: true);

                if (sessionsList.Sessions.Count(s => !string.IsNullOrWhiteSpace(s.IpAddress) && s.IpAddress.Equals(ip)) > Constants.MaxSessionsPerIpAddress)
                {
                    Console.WriteLine("Too many connections from this address.");
                    return;
                }

                using (var stream = client.GetStream())
                {
                    var userRepo = DI.GetRepository<User>();

                    session = new BbsSession(sessionsList, _logger)
                    {
                        User = null,
                        UserRepo = userRepo,
                        UcFlagRepo = DI.GetRepository<UserChannelFlag>(),
                        SysControl = sysControl,
                        Stream = stream,
                        Messager = nodeParams.Messager,
                        IpAddress = ip,
                        PingType = PingPongType.Invisible,
                        CurrentLocation = Module.Connecting
                    };
                    session.ShowPrompt = () => Prompt(session);
                    session.OnPingPong = () =>
                    {
                        if (session.PingType == PingPongType.Full)
                            session.ShowPrompt();
                    };
                    session.Io = new Ascii(session);

                    Logon(session, userRepo);

                    if (session.User != null)
                    {
                        //_logger.Log(session, "User logged in", consoleOnly: true);
                        if (sysControl.HasFlag(SystemControlFlag.AdministratorLoginOnly) && !session.User.Access.HasFlag(AccessFlag.Administrator))
                            session.Io.OutputLine("Sorry the system is currently in maintenence mode, only system administators may log in at this time.  Please try again later.");
                        else
                        {
                            TermSetup.Execute(session);
                            ShowNotifications(session);

                            int unreadMail = Commands.Mail.CountUnread(session);
                            if (unreadMail > 0)
                            {
                                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
                                {
                                    session.Io.OutputLine($"You have {unreadMail} unread mails.  Use '/mail' to read your mail.");
                                }
                            }

                            try
                            {
                                RunSession(session);
                            }
                            finally
                            {
                                session.Messager.Publish(new UserLoginOrOutMessage(session.User, session.Id, false));
                            }
                        }
                        //_logger.Log(session, "User logged out", consoleOnly: true);
                        _logger.Flush();
                    }

                    // wait a small moment before disposing of the stream to allow any buffered output to ... put out!
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    while (sw.ElapsedMilliseconds < Constants.HangupDelay)
                    {
                        Thread.Sleep(5);
                    }
                    sw.Stop();
                }
            } 
            catch (Exception ex)
            {
                _logger.Log($"{DateTime.Now} - {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
            finally
            {
                nodeParams.Client.Close();
                session?.Dispose();
                _logger.Flush();
            }
        }

        private static bool FitsMask(string ip, string mask)
        {
            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(mask))
                return false;
            var ipParts = ip.Split('.');
            var maskParts = mask.Split('.');

            bool fits = true;
            for (int i=0; fits && i < ipParts.Length && i < maskParts.Length; i++)
            {
                var ipPart = ipParts[i];
                var maskPart = maskParts[i];
                fits &= ipPart == maskPart || maskPart == "*";
            }

            return fits;
        }

        private static void ShowNotifications(BbsSession session)
        {
            var notifications = DI.Get<INotificationHandler>().GetNotifications(session.User.Id);
            if (true == notifications?.Any())
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    session.Io.OutputLine($"{Environment.NewLine} ** Listen very carefully, I shall say this only once! ** ");
                }
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Green))
                {
                    string text = string.Join(Environment.NewLine, notifications.Select(n => $"{n.DateSentUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm} {n.Message}"));
                    session.Io.OutputLine(text);
                }
            }
        }

        private static void RunSession(BbsSession session)
        {
            if (session.User.TotalLogons == 1)
            {
                session.CurrentLocation = Module.FauxMain;
                if (!FauxMain.Execute(session))
                {
                    session.Io.OutputLine("Goodbye!");
                    session.Stream.Close();
                    return;
                }    
            }

            session.CurrentLocation = Module.Chat;

            bool notifiedAboutHowToDeleteOwnMessages = false;
            bool notifiedAboutNoOneInChannel = false;

            // set up multi-node subscriptions
            session.ChannelPostSubscriber = new ChannelPostSubscriber(session.Id);
            session.Messager.Subscribe(session.ChannelPostSubscriber);
            session.UserLoginOrOutSubscriber = new UserLoginOrOutSubscriber(session.Id);
            session.Messager.Subscribe(session.UserLoginOrOutSubscriber);
            session.ChannelMessageSubscriber = new ChannelMessageSubscriber(session.Id);
            session.Messager.Subscribe(session.ChannelMessageSubscriber);
            session.UserMessageSubscriber = new UserMessageSuibscriber(session.Id, session.User.Id);
            session.Messager.Subscribe(session.UserMessageSubscriber);
            session.EmoteSubscriber = new EmoteSubscriber();
            session.Messager.Subscribe(session.EmoteSubscriber);

            session.Usernames = session.UserRepo.Get().ToDictionary(k => k.Id, v => v.Name);
            var channelRepo = DI.GetRepository<Channel>();
            var chatRepo = DI.GetRepository<Chat>();

            DatabaseMaint.RemoveSuperfluousUserChannelFlags(session.UcFlagRepo, session.User.Id);

            if (!SwitchOrMakeChannel.Execute(session, Constants.DefaultChannelName))
            {
                throw new Exception($"Unable to switch to '{Constants.DefaultChannelName}' channel.");
            }
            
            session.ChannelPostSubscriber.OnMessageReceived = m => NotifyNewPost(m.Chat, session);
            session.UserLoginOrOutSubscriber.OnMessageReceived = m => NotifyUserLoginOrOut(m.User, session, m.IsLogin);
            session.ChannelMessageSubscriber.OnMessageReceived = m => NotifyChannelMessage(session, m);
            session.UserMessageSubscriber.OnMessageReceived = m => NotifyUserMessage(session, m.Message);
            session.EmoteSubscriber.OnMessageReceived = m => NotifyEmote(session, m);

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Green))
            {
                session.StartPingPong(Constants.DefaultPingPongDelayMin);
                session.Io.OutputLine($"Welcome to Mutiny Community version {Constants.Version}.");
                session.Io.OutputLine("Type '/?' for help (DON'T FORGET THE SLASH!!!!).");
                session.Io.SetForeground(ConsoleColor.Cyan);
                session.Io.OutputLine(" ------------------- ");
                session.Io.OutputLine("Feel free to hang out as long as you want, there is no time limit!");
                session.Io.OutputLine(" ------------------- ");
                session.Io.SetForeground(ConsoleColor.Green);
            }

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
            {
                if (session.User.Timezone == 0)
                    session.Io.OutputLine("All times are in Universal Coordinated Time (UTC), AKA Greenwich mean time (GMT), AKA Zulu time.  Use command /tz to change this.");
                else
                    session.Io.OutputLine($"All times are shown in UTC offset by {session.User.Timezone} hours.  Use /tz to change this.");

                session.Io.SetForeground(ConsoleColor.Magenta);
                session.Io.OutputLine("Press Enter/Return to read next message.");
            }

            while (!session.ForceLogout && session.Stream.CanRead && session.Stream.CanWrite)
            {
                session.ShowPrompt();

                string line = session.Io.InputLine();
                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    // enter (advance to next n lines)
                    if (!session.Chats.ContainsKey(session.MsgPointer))
                    {
                        using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
                        {
                            session.Io.OutputLine("No more messages in this channel.");
                        }
                    }
                    else
                    {
                        Chat nextMessage = session.Chats[session.MsgPointer];
                        nextMessage.Write(session);

                        if (!SetMessagePointer.Execute(session, session.MsgPointer + 1))
                        {
                            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                            {
                                session.Io.OutputLine("No more messages in this channel.");
                            }
                        }
                    }
                }
                else if (line[0] == '/')
                    ExecuteCommand(session, line);
                else if (session.UcFlag.Flags.HasFlag(UCFlag.ReadyOnly) && (session.User.Access & (AccessFlag.Administrator | AccessFlag.GlobalModerator)) == 0)
                    session.Io.OutputLine("You are not allowed to talk in this channel at this time.");
                else if (line?.Length == 1)
                {
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                    {
                        session.Io.OutputLine("Please put a slash in front of a command.  Type /? for help.");
                        session.Io.OutputLine("Anything else you type that doesn't start with a '/' will be entered as a chat message in the current channel.");
                    }
                }
                else if (line.IsPrintable())
                {
                    // only add to chat if the line contains letters, numbers, punctionation
                    // in other words, filter out control characters only such as backspaces.  You would think 
                    // IsNullOrWhitespace would do this but it doesn't

                    Chat chat = AddToChatLog(session, chatRepo, line);

                    if (!notifiedAboutNoOneInChannel && !DI.Get<ISessionsList>().Sessions.Any(s => s.Channel?.Id == session.Channel.Id && s.User?.Id != session.User.Id))
                    {
                        notifiedAboutNoOneInChannel = true;
                        using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                        {
                            string msg =
                                $"Tutor:{Environment.NewLine}" +
                                $"There's no one else on the channel right now but don't fret your message will be shown to users in the future when they log in.{Environment.NewLine}" +
                                "Type /who to see who all is online and in what channels.";
                            session.Io.OutputLine(msg);
                        }
                    }

                    if (!notifiedAboutHowToDeleteOwnMessages && chat.Message.Length < 4)
                    {
                        notifiedAboutHowToDeleteOwnMessages = true;
                        using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                        {
                            string msg =
                                $"Tutor:{Environment.NewLine}" +
                                $"You just entered a message '{chat.Message}' into the chat.{Environment.NewLine}" +
                                $"Did you intend to execute a command?  All commands start with a slash (/).{Environment.NewLine}" +
                                $"Type /? for help.  Type /d to delete your message, '{chat.Message}', from the channel.{Environment.NewLine}" +
                                "This notification will not be shown again during this session.";
                            session.Io.OutputLine(msg);
                        }
                    }
                }
            }

        }

        private static Chat AddToChatLog(BbsSession session, IRepository<Chat> chatRepo, string line, bool isNewTopic = false)
        {
            Chat chat = new Chat
            {
                DateUtc = DateTime.UtcNow,
                ChannelId = session.Channel.Id,
                FromUserId = session.User.Id,
                Message = line,
                ResponseToId = isNewTopic ? null : session.LastReadMessageNumberWhenStartedTyping ?? session.LastReadMessageNumber
            };

            bool isAtEndOfMessages = true != session.Chats?.Any() || session.MsgPointer == session.Chats.Keys.Max();
            chat = chatRepo.Insert(chat);
            //_logger.Log(session, $"posted {chat.Id} in {session.Channel.Name}", consoleOnly: true);            
            session.Chats[chat.Id] = chat;
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
            {
                session.Io.OutputLine($"Message {session.Chats.ItemNumber(chat.Id)} Posted to {session.Channel.Name}.");
                session.LastReadMessageNumber = chat.Id;
            }
            //chat.Write(session);
            if (isAtEndOfMessages)
                SetMessagePointer.Execute(session, chat.Id);
            session.Messager.Publish(new ChannelPostMessage(chat, session.Id));
            return chat;
        }

        private static void Prompt(BbsSession session)
        {
            int highMessage = (true == session.Chats?.Keys?.Any()) ? session.Chats.Keys.Max() : 0;
            var unreadMessageCount = highMessage > 0 ? session.Chats.Count(c => c.Key > session.MsgPointer) : 0;
            if (highMessage > 0 && session.LastReadMessageNumber != highMessage)
                unreadMessageCount++;

            session.Io.SetForeground(ConsoleColor.Cyan);            
            session.Io.Output($"(/?=help) ({unreadMessageCount}){(session.Cols <= 40 ? Environment.NewLine : " ")}<{DateTime.UtcNow.AddHours(session.TimeZone):HH:mm}> [{session.Channel.Id}:{session.Channel.Name}] ");
            session.Io.SetForeground(ConsoleColor.White);
        }

        /// <summary>
        /// a notification that another user has just posted a message in the channel 
        /// well, *a* channel
        /// </summary>
        private static void NotifyNewPost(Chat chat, BbsSession session)
        {
            if (chat == null || chat.ChannelId != session.Channel.Id)
                return;
            
            bool isAtEndOfMessages = session.MsgPointer == session.Chats.Keys.Max();
            session.Chats[chat.Id] = chat;

            Action action = () =>
            {
                TryBell(session, chat.FromUserId);
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                {
                    session.Io.Output($"{Environment.NewLine}Now: ");
                    chat.Write(session);
                    if (isAtEndOfMessages)
                        SetMessagePointer.Execute(session, chat.Id);
                }
            };

            if (session.DoNotDisturb)
                session.DndMessages.Enqueue(action);
            else if (session.Io.IsInputting)
                session.Io.DelayNotification(action);
            else
            {
                action();
                session.ShowPrompt();
            }
        }

        private static void TryBell(BbsSession session, int userId)
        {
            if (string.IsNullOrWhiteSpace(session.BellAlerts))
                return;
            if ("on".Equals(session.BellAlerts, StringComparison.CurrentCultureIgnoreCase) || 
               (session.Usernames.ContainsKey(userId) && session.Usernames[userId].Equals(session.BellAlerts, StringComparison.CurrentCultureIgnoreCase)))
                Bell.Notify(session);
        }

        /// <summary>
        /// a notification about the channel (not a post in the channel) such as so-and-so has joined, or quit, or a message was deleted etc...
        /// </summary>
        private static void NotifyChannelMessage(BbsSession session, ChannelMessage message)
        {
            if (message.ChannelId != session.Channel.Id)
                return;

            Action action = () =>
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                {
                    session.Io.OutputLine($"{Environment.NewLine}{message.Message}");
                    message.OnReceive?.Invoke(session);
                }
            };

            if (session.DoNotDisturb && !message.Disturb)
                session.DndMessages.Enqueue(action);
            else if (session.Io.IsInputting)
                session.Io.DelayNotification(action);
            else
            {
                action();
                session.ShowPrompt();
            }
        }

        /// <summary>
        /// a notification sent to a specific user such as "you have been invited to join some channel"
        /// </summary>
        private static void NotifyUserMessage(BbsSession session, string message)
        {
            Action action = () =>
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                {
                    session.Io.OutputLine($"{Environment.NewLine}{message}");
                }
            };

            if (session.DoNotDisturb)
                session.DndMessages.Enqueue(action);
            else if (session.Io.IsInputting)
                session.Io.DelayNotification(action);
            else
            {
                action();
                session.ShowPrompt();
            }
        }

        private static void NotifyUserLoginOrOut(User user, BbsSession session, bool isLogin)
        {
            if (user == null)
                return;

            Action action = () =>
            {
                TryBell(session, user.Id);
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                {
                    var sessionsForThisUser = DI.Get<ISessionsList>()?.Sessions
                        ?.Where(s => user.Id == s.User?.Id)
                        ?.Count();

                    string message = $"{Environment.NewLine}{user.Name} has {(isLogin ? "logged in" : "logged out")} at {DateTime.UtcNow.AddHours(session.TimeZone):HH:mm:ss}";
                    if (sessionsForThisUser > 1)
                        message += $" ({sessionsForThisUser})";

                    session.Io.OutputLine(message);
                }
            };

            if (session.DoNotDisturb)
                session.DndMessages.Enqueue(action);
            else if (session.Io.IsInputting)
                session.Io.DelayNotification(action);
            else
            {
                action();
                session.ShowPrompt();
            }
        }

        private static void NotifyEmote(BbsSession session, EmoteMessage message)
        {
            if (session.User == null)
                return;
            if (message.ChannelId != session.Channel?.Id)
                return;
            if (message.FromUserId == session.User.Id)
                return;
            if (message.TargetUserId.HasValue && message.TargetUserId.Value != session.User.Id)
                return;

            Action action = () =>
            {
                TryBell(session, message.FromUserId);
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Green))
                {
                    session.Io.OutputLine($"{Environment.NewLine}{message.Message}");
                }
            };

            if (session.DoNotDisturb)
                session.DndMessages.Enqueue(action);
            else if (session.Io.IsInputting)
                session.Io.DelayNotification(action);
            else
            {
                action();
                session.ShowPrompt();
            }
        }

        private static void Logon(BbsSession session, IRepository<User> userRepo)
        {
            session.CurrentLocation = Module.Login;

            if (DI.Get<ISessionsList>().Sessions.Count() >= Constants.MaxSessions)
            {
                session.Io.OutputLine("Sorry, too many people are online right now!  Try again later.");
                Console.WriteLine($"{session.IpAddress} tried to log on but there are too many people online right now.");
                return;
            }

            session.Io.Output($"{Environment.NewLine}press backspace/delete: ");
            var emuTest = session.Io.InputKey();
            session.Io.OutputLine();
            if (emuTest == (char)20)
            {
                session.Io = new Cbm(session);
                session.Cols = 40;
                session.Io.SetColors(ConsoleColor.Black, ConsoleColor.Green);
                session.Io.OutputLine("Commodore PETSCII (CBM) mode activated.");
            }            

            session.Io.Output("Who are you?: ");            
            string username = session.Io.InputLine();
            session.Io.OutputLine();
            if (string.IsNullOrWhiteSpace(username))
            {
                session.Io.OutputLine("Well, goodbye then!");
                return;
            }
            username = username.Trim().ToUpperFirst();
            if (username.Any(c => !char.IsLetter(c)))
            {
                session.Io.OutputLine("Your username must include of only letters.");
                return;
            }

            var user = userRepo.Get(x => x.Name, username)?.FirstOrDefault();
            if (user == null)
            {
                if (username.Length < Constants.MinUsernameLength ||
                    username.Length > Constants.MaxUsernameLength ||
                    Constants.IllegalUsernames.Contains(username, StringComparer.CurrentCultureIgnoreCase))
                {
                    session.Io.OutputLine($"Not allowed to use username {username}.  Minimum Length: {Constants.MinUsernameLength}, Maximum Length: {Constants.MaxUsernameLength}, only letters, and some names just aren't allowed at all.");
                    session.Io.Flush();
                    return;
                }

                session.Io.Output("I've never seen you before, you new here?: ");
                var k = session.Io.InputKey();
                session.Io.OutputLine();
                if (k != 'y' && k != 'Y')
                    return;
                user = RegisterNewUser(session, username, userRepo);
                session.CurrentLocation = Module.Login;
                if (user == null)
                    return;
                session.Io.Output("Do you want to read the new user documentation now?: ");
                k = session.Io.InputKey();
                session.Io.OutputLine();
                if (k == 'y' || k == 'Y')
                    ReadFile.Execute(session, Constants.Files.NewUser);
                else
                    session.Io.OutputLine("Once you get to the main prompt, type '/newuser' to read the new user documentation.  It can be very helpful for new users as this system works differently than most.");

                session.User = user;
            }
            else
            {
                session.Io.Output("Oh yeah? prove it! (password): ");
                string pw = session.Io.InputLine('*')?.ToLower();
                if (!DI.Get<IHasher>().VerifyHash(pw, user.PasswordHash))
                {
                    session.Io.OutputLine("I don't think so.");
                    return;
                }
                if (!user.Access.HasFlag(AccessFlag.MayLogon))
                {
                    session.Io.OutputLine("Your account is currently suspended, you must have pissed someone off!");
                    return;
                }
                if (DI.Get<ISessionsList>().Sessions.Count(s => true == s.User?.Name?.Equals(user.Name, StringComparison.CurrentCultureIgnoreCase)) > Constants.MaxSessionsPerUser)
                {
                    session.Io.OutputLine($"You already have {Constants.MaxSessionsPerUser} sessions going at once!");
                    return;
                }
                session.Io.OutputLine($"Your last logon was at {user.LastLogonUtc} (utc).");
                user.LastLogonUtc = DateTime.UtcNow;
                user.TotalLogons++;
                user = userRepo.Update(user);
                session.Io.OutputLine($"Total logons (including this one): {user.TotalLogons}.");

                session.User = user;                
            }

            if (session.User.Timezone != 0)
                Commands.TimeZone.Execute(session, session.User.Timezone.ToString());

            session.Messager.Publish(new UserLoginOrOutMessage(session.User, session.Id, true));
        }

        private static User RegisterNewUser(BbsSession session, string username, IRepository<User> userRepo)
        {
            session.CurrentLocation = Module.NewUserRegistration;

            session.Io.Output("Choose a password (and don't forget it): ");
            string pw = session.Io.InputLine('*')?.ToLower();

            if (string.IsNullOrWhiteSpace(pw) || pw.Length < Constants.MinimumPasswordLength)
            {
                session.Io.OutputLine($"Password too short, must be at least {Constants.MinimumPasswordLength} characters.");
                return null;
            }

            var now = DateTime.UtcNow;

            User user = new User
            {
                Name = username,
                PasswordHash = DI.Get<IHasher>().Hash(pw),
                DateAddedUtc = now,
                LastLogonUtc = now,
                TotalLogons = 1,
                Access = AccessFlag.MayLogon
            };

            ReadFile.Execute(session, Constants.Files.NewUser);

            user = userRepo.Insert(user);
            return user;
        }

        private static void ExecuteCommand(BbsSession session, string command)
        {
            command = command?.Replace(Environment.NewLine, "");

            if (string.IsNullOrWhiteSpace(command))
                return;

            if (command.StartsWith("/s/", StringComparison.CurrentCultureIgnoreCase))
            {
                // replace "/s/(search)/(replace)" with "/s (search)/(replace)"
                var chars = command.ToArray();
                chars[2] = ' ';
                command = new string(chars);
            }

            var parts = command.Split(' ');
            if (parts?.Length < 1)
                return;

            command = parts[0].ToLower();

            switch (command)
            {
                case "/v":
                case "/ver":
                case "/version":
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
                    {
                        session.Io.OutputLine(Constants.Version);
                    }
                    return;
                case "/o":
                    // logoff
                    session.Io.OutputLine("Goodbye!");
                    session.Stream.Close();
                    return;
                case "/cls":
                case "/clear":
                case "/c":
                    session.Io.ClearScreen();
                    return;
                case "/pass":
                case "/pw":
                case "/password":
                case "/pwd":
                    UpdatePassword.Execute(session);
                    return;
                case "/fauxmain":
                    if (!FauxMain.Execute(session))
                    {
                        // logoff
                        session.Io.OutputLine("Goodbye!");
                        session.Stream.Close();
                    }
                    return;
                case "/term":
                    TermSetup.Execute(session);
                    return;
                case "/bell":
                    Bell.Execute(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                case "/shutdown":
                    ExecuteSessionControlCommand(parts.Skip(1).ToArray(), session);
                    return;
                case "/announce":
                    if (parts.Length >= 2)
                    {
                        Announce.Execute(session, string.Join(" ", parts.Skip(1)));
                        return;
                    }
                    break;
                case "/?":
                    ExecuteMenu(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                case "/a":
                    About.Show(session);
                    return;
                case "/d":
                    {
                        string arg = parts.Length >= 2 ? parts[1] : null;
                        DeleteMessage.Execute(session, arg);
                        return;
                    }
                case "/typo":
                case "/edit":
                case "/s":
                    EditMessage.Execute(session, parts.Skip(1).ToArray());
                    return;
                case "/dnd":
                    session.DoNotDisturb = !session.DoNotDisturb;
                    session.Io.OutputLine($"Do not disturb mode is : {(session.DoNotDisturb ? "On" : "Off")}");
                    return;
                case "/e":
                    SetMessagePointer.Execute(session, session.Chats.Keys.Max());
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                    {
                        session.Io.OutputLine($"Message pointer moved to {session.Chats.ItemNumber(session.MsgPointer)}, press enter to read message.");
                    }
                    return;                
                case "/chl":
                    ListChannels.Execute(session);
                    return;
                case "/ch":
                    ExecuteChannelCommand(session, parts.Skip(1).ToArray());
                    return;
                case "/who":
                    WhoIsOn.Execute(session, DI.Get<ISessionsList>());
                    return;
                case "/w":
                    WhoIsAll.Execute(session);
                    return;
                case "/f":
                    FindMessages.FindByKeyword(session, parts.Length > 1 ? parts[1] : null);
                    return;
                case "/fu":
                    FindMessages.FindBySender(session, parts.Length > 1 ? parts[1] : null);
                    return;
                case "/fs":
                    FindMessages.FindByStartsWith(session, parts.Length > 1 ? parts[1] : null);
                    return;
                case "/afk":
                    Afk.Execute(session, string.Join(" ", parts.Skip(1)));
                    return;
                case "/read":
                    ContinuousRead.Execute(session);
                    return;
                case "/ctx":
                    Commands.Context.Execute(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                case "/new":
                    AddToChatLog(session, DI.GetRepository<Chat>(), string.Join(" ", parts.Skip(1)), isNewTopic: true);
                    return;
                case "/tz":
                    Commands.TimeZone.Execute(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                case "/si":
                    SessionInfo.Execute(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                case "/ui":
                    UserInfo.Execute(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                case "/ci":
                    ChatInfo.Execute(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                case "/cal":
                    Calendar.Execute(session);
                    return;
                case "/index":
                    IndexBy.Execute(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                case "/ipban":
                    if (session.User.Access.HasFlag(AccessFlag.Administrator))
                    {
                        var mask = string.Join(" ", parts.Skip(1));
                        DI.GetRepository<IpBan>().Insert(new IpBan
                        {
                            IpMask = mask
                        });
                        _ipBans.Add(mask);
                        session.Io.OutputLine($"Added '{mask}' to IP ban list.");
                    }
                    return;
                case "/pp":
                    {
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int i))
                            session.StartPingPong(i, silently: false);
                        else
                            session.StartPingPong(0, silently: false);
                    }
                    return;
                case "/newuser":
                    ReadFile.Execute(session, Constants.Files.NewUser);
                    return;
                case "/wave":
                case "/poke":
                case "/smile":
                case "/frown":
                case "/wink":
                case "/nod":
                case "/fairwell":
                case "/farewell":
                case "/goodbye":
                case "/bye":
                    Emote.Execute(session, parts);
                    return;
                case "/roll":
                case "/random":
                case "/rnd":
                case "/dice":
                case "/die":
                    Roll.Execute(session, parts.Skip(1)?.ToArray());
                    return;
                case "/mail":
                    Commands.Mail.Execute(session, parts.Length >= 2 ? parts.Skip(1).ToArray() : null);
                    return;
                case "/feedback":
                    Commands.Mail.Execute(session, "send", Constants.SysopName);
                    return;
                case "/text":
                case "/txt":
                case "/file":
                case "/files":
                case "/filez":
                    {
                        var browser = DI.Get<ITextFilesBrowser>();
                        browser.OnChat = line =>
                        {
                            AddToChatLog(session, DI.GetRepository<Chat>(), line);
                        };
                        browser.Browse(session);
                    }
                    return;
                case "/textread":
                case "/tr":
                    {
                        bool linkFound = false;
                        var browser = DI.Get<ITextFilesBrowser>();
                        Chat msg = null;
                        if (session.LastReadMessageNumber.HasValue && session.Chats.ContainsKey(session.LastReadMessageNumber.Value))
                        {
                            msg = session.Chats[session.LastReadMessageNumber.Value];
                            linkFound = browser.ReadLink(session, msg.Message);
                        }

                        if (!linkFound && session.ContextPointer.HasValue && session.Chats.ContainsKey(session.ContextPointer.Value))
                        {
                            msg = session.Chats[session.ContextPointer.Value];
                            browser.ReadLink(session, msg.Message);
                        }
                    }
                    return;
            }

            if (command.Length > 1 && int.TryParse(command.Substring(1), out int msgNum))
            {
                var n = session.Chats.ItemKey(msgNum);
                if (n.HasValue)
                {
                    SetMessagePointer.Execute(session, n.Value);
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                    {
                        session.Io.OutputLine($"Message pointer moved to {session.Chats.ItemNumber(session.MsgPointer)}, press enter to read message.");
                    }
                }
                else
                {
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                    {
                        session.Io.OutputLine($"Message number {msgNum} is out of range for this channel.");
                    }
                }
            }
            else
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
                {
                    session.Io.OutputLine("Unrecognized command.  Use /? for help.");
                }
            }
        }

        private static void ExecuteChannelCommand(BbsSession session, string[] args)
        {
            if (args.Length == 1)
            {
                switch (args[0].ToLower().Trim())
                {
                    case "+i": ToggleInviteOnly.Execute(session, true); break;
                    case "-i": ToggleInviteOnly.Execute(session, false); break;
                    case "m": ListModerators.Execute(session); break;
                    case "i": ListInvitations.Execute(session); break;
                    case "del": DeleteChannel.Execute(session); break;
                    default: SwitchOrMakeChannel.Execute(session, args[0]); break;
                }
            }
            else if (args.Length == 2)
            {
                switch (args[0].ToLower().Trim())
                {
                    case "i":
                        ToggleChannelInvite.Execute(session, args[1]);
                        break;
                    case "m":
                        ToggleChannelModerator.Execute(session, args[1]);
                        break;
                    case "kick":
                        KickUser.Execute(session, args[1]);
                        break;
                }
                
            }
        }

        private static void ExecuteMenu(BbsSession session, string submenu)
        {
            switch (submenu?.ToLower())
            {
                case "channels":
                    Channels.Show(session);
                    break;
                case "users":
                    Users.Show(session);
                    break;
                case "msgs":
                    Messages.Show(session);
                    break;
                case "context":
                    Menus.Context.Show(session);
                    break;
                case "bells":
                    Menus.Bells.Show(session);
                    break;
                case "emotes":
                    Menus.Emotes.Show(session);
                    break;
                default:
                    MainMenu.Show(session);
                    break;
            }
        }

        private static void ExecuteSessionControlCommand(string[] args, BbsSession session)
        {
            if (args?.Length < 1)
                return;

            switch (args[0].ToLower())
            {
                case "shutdown":
                    if (session.User.Access.HasFlag(AccessFlag.Administrator))
                    Shutdown.Execute(session);
                    break;
            }
        }

    }
}
