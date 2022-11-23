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
using miniBBS.Services.GlobalCommands;
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
        public static SystemControlFlag SysControl = SystemControlFlag.Normal;

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

            var sessionsList = DI.Get<ISessionsList>();
            Console.WriteLine(Constants.Version);
            _ipBans = DI.GetRepository<Core.Models.Data.IpBan>().Get()
                .Select(x => x.IpMask)
                .ToList();

            SysopScreen.Initialize(sessionsList);

            while (!SysControl.HasFlag(SystemControlFlag.Shutdown))
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
                        SysControl = SysControl,
                        Messager = DI.Get<IMessager>()
                    });
                } 
                catch (Exception ex)
                {
                    _logger.Log(null, $"{DateTime.Now} - {ex.Message}{Environment.NewLine}{ex.StackTrace}");
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
                if (true == _ipBans?.Any(x => Commands.IpBan.FitsMask(ip, x)))
                {
                    Console.WriteLine($"Banned ip {ip} rejected.");
                    return;
                }

                if (sessionsList.Sessions.Count(s => !string.IsNullOrWhiteSpace(s.IpAddress) && s.IpAddress.Equals(ip)) > Constants.MaxSessionsPerIpAddress)
                {
                    Console.WriteLine("Too many connections from this address.");
                    return;
                }

                using (var stream = client.GetStream())
                {
                    var userRepo = DI.GetRepository<User>();

                    session = new BbsSession(sessionsList)
                    {
                        User = null,
                        UserRepo = userRepo,
                        UcFlagRepo = DI.GetRepository<UserChannelFlag>(),
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
                        if (sysControl.HasFlag(SystemControlFlag.AdministratorLoginOnly) && !session.User.Access.HasFlag(AccessFlag.Administrator))
                            session.Io.OutputLine("Sorry the system is currently in maintenence mode, only system administators may log in at this time.  Please try again later.");
                        else
                        {
                            TermSetup.Execute(session, cbmDetectedThroughDel: session.Io.EmulationType == TerminalEmulation.Cbm);
                            ShowNotifications(session);

                            int unreadMail = Commands.Mail.CountUnread(session);
                            if (unreadMail > 0)
                            {
                                session.Io.Error($"You have {unreadMail} unread mails.  Use '/mail' to read your mail.");
                                Thread.Sleep(3000);
                            }

                            try
                            {
                                SysopScreen.BeginLogin(session);
                                RunSession(session);
                            }
                            finally
                            {
                                SysopScreen.EndLogin(session);
                                _logger.Log(session, $"{session.User?.Name} has logged out", LoggingOptions.ToDatabase | LoggingOptions.WriteImmedately);
                                session.Messager.Publish(session, new UserLoginOrOutMessage(session, false));
                            }
                        }
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
                // don't bother logging errors caused by "dropped carriers" (disconnects)
                if (!ex.AllExceptions().Any(x =>
                    x.Message.Contains("An established connection was aborted") ||
                    x.Message.Contains("Unable to read data from the transport connection") ||
                    x.Message.Contains("Unable to write data to the transport connection")))
                {
                    _logger.Log(session, $"{DateTime.Now} - {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                }
            }
            finally
            {
                nodeParams.Client.Close();
                session?.RecordSeenData(DI.GetRepository<Metadata>());
                session?.Dispose();
                _logger.Flush();
            }
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
            //else if (session.User.TotalLogons < 10)
            //{
            //    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.DarkGray))
            //    {
            //        session.Io.OutputLine("Since this is not your first call the faux-main menu is skipped.  Use /main if you want to see it again.");
            //    }
            //}

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
            session.GlobalMessageSubscriber = new GlobalMessageSubscriber(session.Id);
            session.Messager.Subscribe(session.GlobalMessageSubscriber);

            session.Usernames = session.UserRepo.Get().ToDictionary(k => k.Id, v => v.Name);
            var channelRepo = DI.GetRepository<Channel>();
            var chatRepo = DI.GetRepository<Chat>();

            DatabaseMaint.RemoveSuperfluousUserChannelFlags(session.UcFlagRepo, session.User.Id);
            
            session.ChannelPostSubscriber.OnMessageReceived = m => NotifyNewPost(m.Chat, session);
            session.UserLoginOrOutSubscriber.OnMessageReceived = m => NotifyUserLoginOrOut(session, m);
            session.ChannelMessageSubscriber.OnMessageReceived = m => NotifyChannelMessage(session, m);
            session.GlobalMessageSubscriber.OnMessageReceived = m => NotifyGlobalMessage(session, m);
            session.UserMessageSubscriber.OnMessageReceived = m => NotifyUserMessage(session, m);
            session.EmoteSubscriber.OnMessageReceived = m => NotifyEmote(session, m);

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Green))
            {
                session.StartPingPong(Constants.DefaultPingPongDelayMin);
                session.Io.OutputLine($"Welcome to Mutiny Community version {Constants.Version}.");
                session.Io.OutputLine("Type '/?' for help (DON'T FORGET THE SLASH!!!!).");
                session.Io.SetForeground(ConsoleColor.Cyan);
                session.Io.OutputLine(" ------------------- ");
                session.Io.OutputLine("Feel free to hang out as long as you want, there is no time limit!");
                Blurbs.Execute(session);
                session.Io.OutputLine(" ------------------- ");
                session.Io.SetForeground(ConsoleColor.Green);
                Thread.Sleep(1000);
            }

            var startupMode = session.User.GetStartupMode(DI.GetRepository<Metadata>());

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
            {
                if (session.User.Timezone == 0)
                    session.Io.OutputLine("All times are in Universal Coordinated Time (UTC), AKA Greenwich mean time (GMT), AKA Zulu time.  Use command /tz to change this.");
                else
                    session.Io.OutputLine($"All times are shown in UTC offset by {session.User.Timezone} hours.  Use /tz to change this.");

                var calCount = Calendar.GetCount();
                if (calCount > 0)
                {
                    session.Io.SetForeground(ConsoleColor.Red);
                    session.Io.OutputLine($"There are {calCount} live chat sessions on the calendar.  Use '/cal' to view them.");
                }

                session.Io.SetForeground(ConsoleColor.Magenta);

                if (!SwitchOrMakeChannel.Execute(session, Constants.DefaultChannelName, allowMakeNewChannel: false, fromMessageBase: startupMode == LoginStartupMode.MainMenu))
                {
                    throw new Exception($"Unable to switch to '{Constants.DefaultChannelName}' channel.");
                }

                session.Io.OutputLine("Press Enter/Return to read next message.");
            }

            OneTimeQuestions.Execute(session);
            startupMode = session.User.GetStartupMode(DI.GetRepository<Metadata>());

            if (startupMode == LoginStartupMode.MainMenu)
            {
                session.CurrentLocation = Module.FauxMain;
                if (!FauxMain.Execute(session))
                {
                    session.Io.OutputLine("Goodbye!");
                    session.Stream.Close();
                    return;
                }
            }

            while (!session.ForceLogout && session.Stream.CanRead && session.Stream.CanWrite)
            {
                session.ShowPrompt();
                string line = session.Io.InputLine(Constants.ChatInputHandling);

                line = line?.Trim();
                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    // enter (advance to next n lines)
                    ShowNextMessage.Execute(session, ChatWriteFlags.UpdateLastReadMessage | ChatWriteFlags.UpdateLastMessagePointer);
                }
                else if (line[0] == '/' || Constants.LegitOneCharacterCommands.Contains(line[0]))
                    ExecuteCommand(session, line);
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

                    Chat chat = AddToChatLog.Execute(session, line);
                    if (chat != null)
                    {
                        if (!notifiedAboutNoOneInChannel && !DI.Get<ISessionsList>().Sessions.Any(s => s.Channel?.Id == session.Channel.Id && s.User?.Id != session.User.Id))
                        {
                            notifiedAboutNoOneInChannel = true;
                            Tutor.Execute(session,                                
                                $"There's no one else on the channel right now but don't fret your message will be shown to users in the future when they log in.{Environment.NewLine}" +
                                "Type /who to see who all is online and in what channels.");
                        }

                        if (!notifiedAboutHowToDeleteOwnMessages && chat.Message.Length < 4)
                        {
                            notifiedAboutHowToDeleteOwnMessages = true;
                            Tutor.Execute(session,
                                $"You just entered a message '{chat.Message}' into the chat.{Environment.NewLine}" +
                                $"Did you intend to execute a command?  All commands start with a slash (/).{Environment.NewLine}" +
                                $"Type /? for help.  Type /d to delete your message, '{chat.Message}', from the channel.{Environment.NewLine}" +
                                "This notification will not be shown again during this session.");
                        }
                    }
                }
            }
        }

        private static void Prompt(BbsSession session)
        {
            session.Io.SetForeground(ConsoleColor.Cyan);
            
            var lastRead = session.Chats.ItemNumber(session.LastMsgPointer) ?? -1;
            var count = session.Chats?.Count-1 ?? 0;
            var chanList = ListChannels.GetChannelList(session);
            
            var chanNum = chanList.IndexOf(c => c.Name == session.Channel.Name) + 1;

            var prompt = 
                $"{DateTime.UtcNow.AddHours(session.TimeZone):HH:mm}" + 
                UserIoExtensions.WrapInColor(", ", ConsoleColor.DarkGray) +
                UserIoExtensions.WrapInColor(lastRead.ToString(), lastRead == count ? ConsoleColor.Cyan : ConsoleColor.Magenta) +
                UserIoExtensions.WrapInColor("/", ConsoleColor.DarkGray) +
                $"{count}" +
                UserIoExtensions.WrapInColor(", ", ConsoleColor.DarkGray) +
                $"{chanNum}:{session.Channel.Name} {UserIoExtensions.WrapInColor(">", ConsoleColor.White)} ";

            session.Io.Output(prompt);
            session.Io.SetForeground(ConsoleColor.White);
        }

        /// <summary>
        /// a notification that another user has just posted a message in the channel 
        /// well, *a* channel
        /// </summary>
        private static void NotifyNewPost(Chat chat, BbsSession session)
        {
            if (chat == null || chat.ChannelId != session.Channel.Id || session.IsIgnoring(chat.FromUserId))
                return;

            int lastRead =
                session.LastMsgPointer ??
                //session.LastReadMessageNumberWhenStartedTyping ??
                //session.LastReadMessageNumber ??
                session.MsgPointer;

            bool isAtEndOfMessages = true == session.Chats?.Any() && lastRead == session.Chats.Keys[session.Chats.Keys.Count - 2];

            Action action = () =>
            {
                TryBell(session, chat.FromUserId);
                //using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                //{
                //    session.Io.Output($"{Environment.NewLine}Now: ");
                chat.Write(session, ChatWriteFlags.UpdateLastReadMessage | ChatWriteFlags.LiveMessageNotification);
                if (isAtEndOfMessages)
                {
                    SetMessagePointer.Execute(session, chat.Id);
                    session.LastMsgPointer = session.MsgPointer;
                }
                //}
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
            if (string.IsNullOrWhiteSpace(session.BellAlerts) || session.IsIgnoring(userId))
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
        /// a notification to any user in any channel
        /// </summary>
        private static void NotifyGlobalMessage(BbsSession session, GlobalMessage message)
        {
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
        private static void NotifyUserMessage(BbsSession session, UserMessage message)
        {
            var fromSession = DI.Get<ISessionsList>().Sessions.FirstOrDefault(s => s.Id == message.SessionId);
            var fromUserId = fromSession?.User?.Id;
            if (fromUserId.HasValue && session.IsIgnoring(fromUserId.Value))
                return;

            Action action = () =>
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, message.TextColor))
                {
                    session.Io.OutputLine($"{Environment.NewLine}{message.Message}");
                }
                message.AdditionalAction?.Invoke(session);
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

        private static void NotifyUserLoginOrOut(BbsSession session, UserLoginOrOutMessage message)
        {
            if (message?.User == null)
                return;

            Action action = () =>
            {
                TryBell(session, message.User.Id);
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                {
                    var sessionsForThisUser = DI.Get<ISessionsList>()?.Sessions
                        ?.Where(s => message.User.Id == s.User?.Id)
                        ?.Count();

                    string msg = $"{Environment.NewLine}{message.User.Name}{(sessionsForThisUser > 1 ? $" ({sessionsForThisUser})" : "")} has {(message.IsLogin ? "logged in" : "logged out")} at {DateTime.UtcNow.AddHours(session.TimeZone):HH:mm}";
                    if (!string.IsNullOrWhiteSpace(message.LogoutMessage))
                        msg += $" saying \"{message.LogoutMessage}\"";

                    session.Io.OutputLine(msg);
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
            if (session.User == null ||
                message.ChannelId != session.Channel?.Id ||
                message.FromUserId == session.User.Id ||
                (message.TargetUserId.HasValue && message.TargetUserId.Value != session.User.Id) ||
                session.IsIgnoring(message.FromUserId))
            {
                return;
            }

            Action action = () =>
            {
                TryBell(session, message.FromUserId);
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
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
                _logger.Log(session, $"{session.IpAddress} tried to log on but there are too many people online right now.");
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
                session.Io.OutputLine("Your username must include only letters.");
                return;
            }

            if ("Guest".Equals(username, StringComparison.CurrentCultureIgnoreCase))
            {
                session.Io.Error("Guest logins not allowed.  All you need to register is to specify a username and a password, I'm not going to ask for your phone number, address, email address, who referred you, you social security number, your favorite flavor of ice cream, etc... We just need to make an account for you to log in with and don't really care about anything personal.");
                return;
            }

            var user = userRepo.Get(x => x.Name, username)?.FirstOrDefault();
            if (user == null)
            {
                if (username.Length < Constants.MinUsernameLength ||
                    username.Length > Constants.MaxUsernameLength ||
                    username.Any(c => !char.IsLetter(c)) ||
                    Constants.IllegalUsernames.Contains(username, StringComparer.CurrentCultureIgnoreCase))
                {
                    session.Io.OutputLine($"Not allowed to use username {username}.  Minimum Length: {Constants.MinUsernameLength}, Maximum Length: {Constants.MaxUsernameLength}, only letters, and some names just aren't allowed at all.");
                    session.Io.Flush();
                    return;
                }

                var k = session.Io.Ask("I've never seen you before, you new here?");
                if (k != 'Y')
                    return;
                user = RegisterNewUser(session, username, userRepo);
                session.CurrentLocation = Module.Login;
                if (user == null)
                    return;
                k = session.Io.Ask("Do you want to read the new user documentation now?");
                if (k == 'Y')
                    ReadFile.Execute(session, Constants.Files.NewUser);
                else
                    session.Io.OutputLine("Once you get logged in, type '/newuser' to read the new user documentation.  It can be very helpful for new users as this system works differently than most.");

                session.User = user;
            }
            else
            {
                session.Io.Output("Oh yeah? prove it! (password): ");
                string pw = session.Io.InputLine(InputHandlingFlag.PasswordInput)?.ToLower();
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

            if (user.Access.HasFlag(AccessFlag.Administrator))
            {
                WhoIsOn.Execute(session);
                var k = session.Io.Ask("Admin login option: (N)ormal, (S)ilent, (I)nvisible");
                switch (k)
                {
                    case 'S':
                        session.ControlFlags |= SessionControlFlags.DoNotSendNotifications;
                        break;
                    case 'I':
                        session.ControlFlags |= SessionControlFlags.Invisible | SessionControlFlags.DoNotSendNotifications;
                        break;
                }
            }

            _logger.Log(session, $"{session.User?.Name} has logged in", LoggingOptions.ToDatabase | LoggingOptions.WriteImmedately);
            session.Messager.Publish(session, new UserLoginOrOutMessage(session, true));
        }

        private static User RegisterNewUser(BbsSession session, string username, IRepository<User> userRepo)
        {
            session.CurrentLocation = Module.NewUserRegistration;

            session.Io.Output("Choose a password (and don't forget it): ");
            string pw = session.Io.InputLine(InputHandlingFlag.PasswordInput)?.ToLower();

            if (string.IsNullOrWhiteSpace(pw) || pw.Length < Constants.MinimumPasswordLength || pw.Length > Constants.MaximumPasswordLength)
            {
                session.Io.OutputLine($"Password too short, must be at least {Constants.MinimumPasswordLength} characters and not more than {Constants.MaximumPasswordLength} characters.");
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

            //ReadFile.Execute(session, Constants.Files.NewUser);

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
                case "/m":
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                    {
                        var _lastRead = session.Chats.ItemNumber(session.LastReadMessageNumber) ?? -1;
                        var _high = session.Chats.Count - 1;
                        string msg = string.Join(Environment.NewLine, new[]
                        {
                            $"{UserIoExtensions.WrapInColor(session.Channel.Name, ConsoleColor.White)} Message Info:",
                            $"High Msg  : {UserIoExtensions.WrapInColor(_high.ToString(), ConsoleColor.White)}",
                            $"Last Read : {UserIoExtensions.WrapInColor(_lastRead.ToString(), ConsoleColor.White)}",
                            $"Unread{UserIoExtensions.WrapInColor("*", ConsoleColor.DarkGray)}   : {UserIoExtensions.WrapInColor((_high - _lastRead).ToString(), ConsoleColor.White)}",
                            UserIoExtensions.WrapInColor("*: High-Last Read = Unread", ConsoleColor.DarkGray)
                        });
                        session.Io.Output(msg);
                    }
                    return;
                case "/msg":
                    Msg.Execute(session);
                    return;
                case "/o":
                case "/off":
                case "/g":
                case "/logoff":
                case "/quit":
                case "/q":
                    if (Logout.Execute(session, parts[0], string.Join(" ", parts.Skip(1))))
                    {
                        session.Io.OutputLine("Goodbye!");
                        session.Stream.Close();
                    }
                    return;
                case "/seen":
                    Seen.Execute(session, parts.Skip(1).ToArray());
                    return;
                case "/cls":
                case "/clr":
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
                case "/main":
                case "/menu":
                case "/fauxmain":
                case "/fauxmenu":
                case "/fakemain":
                case "/fakemenu":
                    if (!FauxMain.Execute(session))
                    {
                        // logoff
                        session.Io.OutputLine("Goodbye!");
                        session.Stream.Close();
                    }
                    return;
                case "/ignore":
                    Ignore.Execute(session, parts.Skip(1).ToArray());
                    return;
                case "/term":
                case "/setup":
                case "/emu":
                    TermSetup.Execute(session);
                    return;
                case "/pref":
                    UserPreferences.Execute(session);
                    return;
                case "/bell":
                case "/sound":
                    Bell.Execute(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                case "/syscontrol":
                case "/systemcontrol":
                case "/sysctrl":
                    if (session.User.Access.HasFlag(AccessFlag.Administrator))
                        SystemControl.Execute(session, parts.Skip(1).ToArray());
                    return;
                case "/announce":
                    if (parts.Length >= 2)
                    {
                        Announce.Execute(session, string.Join(" ", parts.Skip(1)));
                        return;
                    }
                    break;
                case "/help":
                case "/?":
                case "?":
                    ExecuteMenu(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                case "/??":
                    CommandList.Execute(session);
                    return;
                case "/about":
                case "/a":
                    About.Show(session);
                    return;
                case "/del":
                case "/delete":
                case "/d":
                    {
                        string arg = parts.Length >= 2 ? parts[1] : null;
                        DeleteMessage.Execute(session, arg);
                        return;
                    }
                case "/typo":
                case "/edit":
                case "/s":
                    EditMessage.Execute(session, string.Join(" ", parts.Skip(1)));
                    return;
                case "/rere":
                    EditMessage.ReassignReNumber(session, parts.Skip(1).ToArray());
                    return;
                case "/pin":
                    Pin.Execute(session, parts.Skip(1).ToArray());
                    return;
                case "/pins":
                    Pin.ShowPins(session, parts.Skip(1).ToArray());
                    return;
                case "/unpin":
                    Pin.Unpin(session, parts.Skip(1).ToArray());
                    return;
                case "/dnd":
                    session.DoNotDisturb = !session.DoNotDisturb;
                    return;
                case "/e":
                case "/end":
                    SetMessagePointer.Execute(session, session.Chats.Keys.Max());
                    session.Chats[session.MsgPointer].Write(session, ChatWriteFlags.UpdateLastMessagePointer | ChatWriteFlags.UpdateLastReadMessage);
                    return;                
                case "/chl":
                case "/chanlist":
                case "/channellist":
                case "/channelist":
                    ListChannels.Execute(session);
                    return;
                case "/ch":
                case "/chan":
                case "/channel":
                    ExecuteChannelCommand(session, parts.Skip(1).ToArray());
                    return;
                case "/who":
                    WhoIsOn.Execute(session, DI.Get<ISessionsList>());
                    return;
                case "/u":
                case "/users":
                    WhoIsAll.Execute(session, parts.Skip(1).ToArray());
                    return;
                case "/f":
                case "/find":
                case "/search":
                    FindMessages.FindByKeyword(session, parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : null);
                    return;
                case "/fu":
                    FindMessages.FindBySender(session, parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : null);
                    return;
                case "/fs":
                    FindMessages.FindByStartsWith(session, parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : null);
                    return;
                case "/afk":
                    Afk.Execute(session, string.Join(" ", parts.Skip(1)));
                    return;
                case "/read":
                case "/nonstop":
                    ContinuousRead.Execute(session);
                    return;
                case "/ctx":
                case "/cx":
                case "/re":
                case "/ref":
                case "/wat":
                    Commands.Context.Execute(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                case "/ra":
                    Commands.Context.Execute(session, ">");
                    return;
                case "/new":
                    AddToChatLog.Execute(session, string.Join(" ", parts.Skip(1)), PostChatFlags.IsNewTopic);
                    return;
                case "/tz":
                case "/timezone":
                case "/time":
                    Commands.TimeZone.Execute(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                case "/si":
                case "/session":
                case "/sessioninfo":
                    SessionInfo.Execute(session, parts.Skip(1).ToArray());
                    return;
                case "/times":
                case "/dates":
                case "/date":
                case "/when":
                    SessionInfo.Execute(session, "times");
                    return;
                case "/ui":
                case "/user":
                case "/userinfo":
                    UserInfo.Execute(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                case "/ci":
                case "/chat":
                case "/chatinfo":
                    ChatInfo.Execute(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                case "/cal":
                case "/calendar":
                    Calendar.Execute(session);
                    return;
                case "/calc":
                case "/calculate":
                case "/calculator":
                    Calculate.Execute(session, parts.Skip(1).ToArray());
                    return;
                case "/index":
                case "/i":
                    IndexBy.Execute(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                case "/ipban":
                    Commands.IpBan.Execute(session, ref _ipBans, parts.Skip(1).ToArray());
                    return;
                case "/pp":
                case "/keepalive":
                case "/ping":
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
                case "/me":
                case "/online":
                case "/onl":
                case "/on":
                    Emote.Execute(session, parts);
                    return;
                case "/whisper":
                case "/wh":
                case "/w":
                    if (parts.Length >= 3)
                        Whisper.Execute(session, parts.Skip(1).ToArray());
                    else
                        WhoIsOn.Execute(session, DI.Get<ISessionsList>());
                    return;
                case "/r":
                case "/reply":
                    Whisper.Reply(session, parts.Skip(1).ToArray());
                    return;
                case "/roll":
                case "/random":
                case "/rnd":
                case "/dice":
                case "/die":
                    Roll.Execute(session, parts.Skip(1)?.ToArray());
                    return;
                case "/mail":
                case "/email":
                case "/e-mail":
                    Commands.Mail.Execute(session, parts.Skip(1).ToArray());
                    return;
                case "/feedback":
                    Commands.Mail.Execute(session, "send", Constants.SysopName);
                    return;
                case "/texts":
                case "/textz":
                case "/text":
                case "/txt":
                case "/file":
                case "/files":
                case "/filez":
                case "/myfiles":
                    {
                        var browser = DI.Get<ITextFilesBrowser>();
                        browser.OnChat = line =>
                        {
                            AddToChatLog.Execute(session, line);
                        };
                        FilesLaunchFlags flags = 
                            "/myfiles".Equals(command, StringComparison.CurrentCultureIgnoreCase) ? 
                            FilesLaunchFlags.MoveToUserHomeDirectory : 
                            FilesLaunchFlags.ReturnToPreviousDirectory;

                        browser.Browse(session, flags);
                    }
                    return;
                case "/textread":
                case "/tr":
                case "/run":
                case "/exec":
                    ReadTextFile.Execute(session, parts.Skip(1).ToArray());
                    return;
                case "/blurb":
                    Blurbs.Execute(session, string.Join(" ", parts.Skip(1)));
                    return;
                case "/blurbadmin":
                    Blurbs.BlurbAdmin(session, parts.Skip(1).ToArray());
                    return;
                case "/hand":
                case "/raise":
                case "/raisehand":
                case "/voice":
                    VoiceRequestQueueManager.RequestVoice(session);
                    return;
                case "/uptime":
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                    {
                        var communityUptime = DateTime.UtcNow - SysopScreen.StartedAtUtc;
                        session.Io.OutputLine($"Community Uptime: {communityUptime.Days}d {communityUptime.Hours}h {communityUptime.Minutes}m");
                    }
                    return;
                case "/vote":
                case "/votes":
                case "/poll":
                case "/polls":
                    Polls.Execute(session);
                    return;
                case "/game":
                case "/games":
                case "/prog":
                case "/progs":
                case "/door":
                case "/doors":
                    BrowseGames.Execute(session);
                    return;
                case "/sys":
                case "/sysop":
                    SysopCommand.Execute(session, ref _ipBans, parts.Skip(1).ToArray());
                    return;
                case "/basic":
                case "/bas":
                    Commands.Basic.Execute(session, parts.Skip(1).ToArray());
                    return;
                case "/bots":
                    Bot.ListBots(session, parts.Skip(1)?.FirstOrDefault());
                    return;
                case ",":
                case "<":
                    SetMessagePointer.Execute(session, session.MsgPointer - 1, reverse: true);
                    session.Chats[session.MsgPointer].Write(session, ChatWriteFlags.UpdateLastMessagePointer | ChatWriteFlags.UpdateLastReadMessage);
                    return;
                case ".":
                case ">":
                    ShowNextMessage.Execute(session, ChatWriteFlags.UpdateLastReadMessage | ChatWriteFlags.UpdateLastMessagePointer);
                    return;
                case "[":
                case "{":
                    {
                        var chans = new SortedList<int, Channel>(GetChannel.GetChannels(session)
                            .ToDictionary(k => k.Id));
                        var currentChannelNumber = chans.ItemNumber(session.Channel.Id);
                        int? nextChannelNumber = currentChannelNumber.Value - 1;
                        var nextChannelId = chans.ItemKey(nextChannelNumber.Value) ?? chans.Last().Key;
                        nextChannelNumber = chans.ItemNumber(nextChannelId);
                        SwitchOrMakeChannel.Execute(session, $"{nextChannelNumber+1}", false);
                    }
                    return;
                case "]":
                case "}":
                    {
                        var chans = new SortedList<int, Channel>(GetChannel.GetChannels(session)
                            .ToDictionary(k => k.Id));
                        var currentChannelNumber = chans.ItemNumber(session.Channel.Id);
                        int? nextChannelNumber = currentChannelNumber.Value + 1;
                        var nextChannelId = chans.ItemKey(nextChannelNumber.Value) ?? chans.First().Key;
                        nextChannelNumber = chans.ItemNumber(nextChannelId);
                        SwitchOrMakeChannel.Execute(session, $"{nextChannelNumber+1}", false);
                    }
                    return;
            }

            if (command.Length > 1 && int.TryParse(command.Substring(1), out int msgNum))
            {
                var n = session.Chats.ItemKey(msgNum);
                if (n.HasValue)
                {
                    SetMessagePointer.Execute(session, n.Value);
                    ShowNextMessage.Execute(session, ChatWriteFlags.UpdateLastReadMessage | ChatWriteFlags.UpdateLastMessagePointer);
                }
                else
                    session.Io.Error($"Message number {msgNum} is out of range for this channel.");
            }
            else if (command.EndsWith("bot", StringComparison.CurrentCultureIgnoreCase) && command.Length >= 5)
            {
                var scriptName = command.Substring(1, command.Length - 4);
                Bot.Execute(session, scriptName, string.Join(" ", parts.Skip(1)));
            }
            else
                session.Io.Error("Unrecognized command.  Use /? for help.");
        }

        private static void ExecuteChannelCommand(BbsSession session, string[] args)
        {
            if (args.Length == 1)
            {
                switch (args[0].ToLower().Trim())
                {
                    case "+i":
                        ToggleInviteOnly.Execute(session, true); 
                        break;
                    case "-i": 
                        ToggleInviteOnly.Execute(session, false); 
                        break;
                    case "+v":
                    case "-v":
                    case "vq":
                    case "vlist":
                    case "vall":
                    case "vnone":
                        ChannelVoice.Execute(session, args);
                        break;
                    case "m": 
                        ListModerators.Execute(session); 
                        break;
                    case "i": 
                        ListInvitations.Execute(session); 
                        break;
                    case "del": 
                        DeleteChannel.Execute(session); 
                        break;
                    default: 
                        SwitchOrMakeChannel.Execute(session, args[0], allowMakeNewChannel: true); 
                        break;
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
            else if (args.Length >= 3)
            {
                switch (args[0].ToLower().Trim())
                {
                    case "movemsg": MoveMsg.Execute(session, args.Skip(1).ToArray()); break;
                }
            }
        }

        private static void ExecuteMenu(BbsSession session, string submenu)
        {
            switch (submenu?.ToLower())
            {
                case "channels":
                case "chans":
                case "ch":
                    Channels.Show(session);
                    break;
                case "users":
                    Users.Show(session);
                    break;
                case "msgs":
                case "messages":
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
                case "mod":
                case "moderator":
                    Menus.Moderator.Show(session);
                    break;
                case "voice":
                    Menus.Voice.Show(session);
                    break;
                default:
                    MainMenu.Show(session);
                    break;
            }
        }

    }
}
