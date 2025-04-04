using miniBBS.Commands;
using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Exceptions;
using miniBBS.Extensions;
using miniBBS.Helpers;
using miniBBS.Menus;
using miniBBS.Persistence;
using miniBBS.Services;
using miniBBS.Services.GlobalCommands;
using miniBBS.Subscribers;
using miniBBS.UserIo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

            if (Constants.Gopher.ServerEnabled)
            {
                var gopherServerThread = new Thread(new ParameterizedThreadStart(StartGophserServer));
                Console.WriteLine("Starting Gopher Server");
                gopherServerThread.Start(new GopherServerOptions
                {
                    BbsPort = port,
                    GopherServerPort = Constants.Gopher.ServerPort,
                    SystemControl = SysControl,
                });
            }

            TcpListener listener = null;

            try
            {
                if (true == args?.Any(a => "local".Equals(a, StringComparison.CurrentCultureIgnoreCase)))
                    Constants.IsLocal = true;

                _logger = DI.Get<ILogger>();

                new DatabaseInitializer().InitializeDatabase();

                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();

                var sessionsList = DI.Get<ISessionsList>();
                Console.WriteLine(Constants.Version);
                _ipBans = DI.GetRepository<Core.Models.Data.IpBan>().Get()
                    .Select(x => x.IpMask)
                    .ToList();

                SysopScreen.Initialize(sessionsList);
            }
            catch (Exception ex)
            {
                _logger.Log(null, $"{DateTime.Now} (outside of main loop) - {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }

            while (!SysControl.HasFlag(SystemControlFlag.Shutdown))
            {
                try
                {
                    var clientFactory = new TcpClientFactory(listener);
                    clientFactory.AwaitConnection();
                    while (clientFactory.Client == null)
                    {
                        Thread.Sleep(25);
                    }
                    var client = clientFactory.Client;
                    var threadStart = new ParameterizedThreadStart(BeginConnection);
                    var thread = new Thread(threadStart);
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

            listener?.Stop();
        }

        static void StartGophserServer(object o)
        {
            var options = o as GopherServerOptions;
            DI.Get<IGopherServer>().StartServer(options);
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
                    _logger?.Log(null, $"Banned ip {ip} rejected.", LoggingOptions.ToConsole);
                    return;
                }

                SysopScreen.SetLastConnectionIp(ip);

                if (sessionsList.Sessions.Count(s => !string.IsNullOrWhiteSpace(s.IpAddress) && s.IpAddress.Equals(ip)) > Constants.MaxSessionsPerIpAddress)
                {
                    DI.Get<ILogger>().Log(session, $"Too many connections from {session?.IpAddress}.");
                    return;
                }

                using (var stream = client.GetStream())
                {
                    var userRepo = DI.GetRepository<User>();

                    session = new BbsSession(sessionsList, stream, client)
                    {
                        User = null,
                        UserRepo = userRepo,
                        UcFlagRepo = DI.GetRepository<UserChannelFlag>(),
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
                            TerminalEmulation? detectedEmulation = null;
                            if (session.Io.EmulationType == TerminalEmulation.Cbm || session.Io.EmulationType == TerminalEmulation.Atascii)
                            {
                                detectedEmulation = session.Io.EmulationType;
                                session.Cols = 40;
                            }
                            else if (session.Io.EmulationType == TerminalEmulation.Ansi)
                            {
                                detectedEmulation = session.Io.EmulationType;
                            }

                            TermSetup.Execute(session, detectedEmulation);
                            
                            if (!DI.Get<IMenuFileLoader>().TryShow(session, MenuFileType.Login))
                                Banners.Show(session);

                            session.Io.SetBackground(ConsoleColor.Black);
                            session.Io.OutputLine();

                            session.Io.OutputLine(Constants.BbsName.Color(ConsoleColor.Cyan) + $" Version {Constants.Version}.".Color(ConsoleColor.Yellow));
                            
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
                    _logger.Log(session, $"{DateTime.Now} - {ex.Message}{Environment.NewLine}{ex.StackTrace}", LoggingOptions.ToConsole | LoggingOptions.ToDatabase);
                }
            }
            finally
            {
                try
                {
                    nodeParams?.Client?.Close();
                    session?.SaveReads(GlobalDependencyResolver.Default);
                    session?.RecordSeenData(DI.GetRepository<Metadata>());
                    session?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.Log(session, $"{DateTime.Now} - {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                }
                finally
                {
                    _logger?.Flush();
                }
            }
        }

        private static IEnumerable<string> GetOtherNotifications(BbsSession session)
        {
            var notifications = DI.Get<INotificationHandler>().GetNotifications(session.User.Id);
            if (true == notifications?.Any())
            {
                yield return $"{Environment.NewLine} ** Listen very carefully, I shall say this only once! ** ".Color(ConsoleColor.Red);
                foreach (var notification in notifications)
                    $"{notification.DateSentUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm} {notification.Message}"
                        .Color(ConsoleColor.Green);
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
                //session.Io.OutputLine("Type '/?' for help.");
                Blurbs.Execute(session);
                //session.Io.OutputLine(" ------------------- ");
                //session.Io.SetForeground(ConsoleColor.Green);
                Thread.Sleep(500);
            }

            var metaRepo = DI.GetRepository<Metadata>();
            var startupMode = session.User.GetStartupMode(metaRepo);
            session.Items[SessionItem.StartupMode] = startupMode;
            OneTimeQuestions.Execute(session);
            startupMode = session.User.GetStartupMode(metaRepo);
            session.LoadChatHeaderFormat(metaRepo);

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                //if (session.User.Timezone == 0)
                //    session.Io.OutputLine("All times are in Universal Coordinated Time (UTC), AKA Greenwich mean time (GMT), AKA Zulu time.  Use command /tz to change this.");
                //else
                //    session.Io.OutputLine($"All times are shown in UTC offset by {session.User.Timezone} hours.  Use /tz (from chat) to change this.");

                session.Io.SetForeground(ConsoleColor.Magenta);

                if (!SwitchOrMakeChannel.Execute(session, Constants.DefaultChannelName, allowMakeNewChannel: false, fromMessageBase: startupMode == LoginStartupMode.MainMenu))
                {
                    throw new Exception($"Unable to switch to '{Constants.DefaultChannelName}' channel.");
                }
            }

            ShowLoginNotifications(session, startupMode);
            BookmarkManager.CheckBookmarkedRead(session);

            if (startupMode == LoginStartupMode.MainMenu)
            {
                session.CurrentLocation = Module.MainMenu;
                if (!Commands.MainMenu.Execute(session))
                {
                    if (!DI.Get<IMenuFileLoader>().TryShow(session, MenuFileType.Logout))
                        session.Io.OutputLine("Goodbye!");
                    session.Disconnect();
                    Thread.Sleep(100);
                    return;
                }
            }

            session.CurrentLocation = Module.Chat;

            session.Io.OutputLine("Press Enter/Return to read next message.");

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
                else if ((line[0] == '/' || Constants.LegitOneCharacterCommands.Contains(line[0]))
                    && !(line.Length > 1 && line[1] == '/'))
                {
                    ExecuteCommand(session, line);
                }
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

                    if (line.StartsWith("//"))
                        line = line.Substring(1);

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

        private static void ShowLoginNotifications(BbsSession session, LoginStartupMode startupMode)
        {
            const int BULLETINS = 0, CHATS = 1, MAILS = 2, POLLS = 3, CALS = 4;

            session.Io.Output("Gathering statistics...".Color(ConsoleColor.DarkGreen));

            var counts = new Count[]
            {
                Bulletins.Count(session),
                ListChannels.Count(session),
                Commands.Mail.Count(session),
                Polls.Count(session),
                Calendar.Count(session)
            };

            session.Io.OutputLine();
            session.Io.OutputLine("Last 5 users".Color(ConsoleColor.Magenta));
            Seen.Execute(session, "5");

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
            {
                session.Io.Output($"{Constants.Inverser}* {Constants.BbsName} Login Notifications *{Constants.Inverser}");
                session.Io.SetForeground(ConsoleColor.White);
                session.Io.OutputLine();

                var formattedCounts = counts.Select(c =>
                {
                    var color = c.SubsetCount > 0 ? ConsoleColor.Green : ConsoleColor.DarkCyan;
                    return $"{c.SubsetCount.ToString().Color(color)} / {c.TotalCount}";
                }).ToArray();

                if (counts[BULLETINS].SubsetCount > Constants.MaxUnarchivedBulletins)
                {
                    formattedCounts[BULLETINS] = $"more than {Constants.MaxUnarchivedBulletins}";
                }

                if (counts[CHATS].SubsetCount > Constants.MaxUnarchivedChats)
                {
                    formattedCounts[CHATS] = $"more than {Constants.MaxUnarchivedChats}";
                }

                var builder = new StringBuilder();
                if (startupMode == LoginStartupMode.ChatRooms)
                {
                    builder.AppendLine($"{Constants.Spaceholder}".Repeat(6) + $"Unread Bulletins (/b): {formattedCounts[BULLETINS]}");
                    builder.AppendLine($"Unread Chats (all channels): {formattedCounts[CHATS]}");
                    builder.AppendLine($"{Constants.Spaceholder}".Repeat(6) + $"Unread Emails (/mail): {formattedCounts[MAILS]}");
                    builder.AppendLine($"{Constants.Spaceholder}".Repeat(9) + $"New Polls (/polls): {formattedCounts[POLLS]}");
                    builder.AppendLine($"{Constants.Spaceholder}New Calendar Events (/cal): {formattedCounts[CALS]}");
                }
                else
                {
                    builder.AppendLine($"{Constants.Spaceholder}".Repeat(4) + $"Unread Bulletins (B): {formattedCounts[BULLETINS]}");
                    builder.AppendLine($"{Constants.Spaceholder}".Repeat(8) + $"Unread Chats (C): {formattedCounts[CHATS]}");
                    builder.AppendLine($"{Constants.Spaceholder}".Repeat(7) + $"Unread Emails (E): {formattedCounts[MAILS]}");
                    builder.AppendLine($"{Constants.Spaceholder}".Repeat(11) + $"New Polls (V): {formattedCounts[POLLS]}");
                    builder.AppendLine($"{Constants.Spaceholder}New Calendar Events (L): {formattedCounts[CALS]}");
                }
                foreach (var other in GetOtherNotifications(session))
                    builder.AppendLine(other);
                session.Io.Output(builder.ToString());
                Thread.Sleep(1234);
            }

            if (counts[MAILS].SubsetCount > 0 && 'Y' == session.Io.Ask("You have unread e-mail, read now?"))
            {
                Commands.Mail.Execute(session, "list");
            }

            WhoIsOn.Execute(session);
        }

        private static void Prompt(BbsSession session)
        {
            session.Io.SetForeground(ConsoleColor.Cyan);

            if (session.LastMsgPointer.HasValue && session.Chats?.Any() == true && session.LastMsgPointer.Value < session.Chats.Keys.FirstOrDefault())
                session.LastMsgPointer = session.Chats.Keys.First();

            var lastRead = session.Chats.ItemNumber(session.LastMsgPointer) ?? -1;
            var count = session.Chats?.Count-1 ?? 0;
            var chanList = ListChannels.GetChannelList(session);
            
            var chanNum = 
                string.IsNullOrWhiteSpace(session?.Channel?.Name) ? -1 :
                chanList.IndexOf(c => c.Name == session.Channel.Name) + 1;

            var prompt =
                $"{DateTime.UtcNow.AddHours(session.TimeZone):HH:mm}" + 
                UserIoExtensions.WrapInColor(", ", ConsoleColor.DarkGray) +
                UserIoExtensions.WrapInColor(lastRead.ToString(), lastRead == count ? ConsoleColor.Cyan : ConsoleColor.Magenta) +
                UserIoExtensions.WrapInColor("/", ConsoleColor.DarkGray) +
                $"{count}" +
                UserIoExtensions.WrapInColor(", ", ConsoleColor.DarkGray) +
                $"{Constants.Inverser}{chanNum}:{session?.Channel?.Name}{Constants.Inverser} {UserIoExtensions.WrapInColor(">", ConsoleColor.White)} ";

            session.Io.Output(prompt, OutputHandlingFlag.NoWordWrap);
            session.Io.SetForeground(ConsoleColor.White);
        }

        /// <summary>
        /// a notification that another user has just posted a message in the channel 
        /// well, *a* channel
        /// </summary>
        private static void NotifyNewPost(Chat chat, BbsSession session)
        {
            if (chat == null || session.IsIgnoring(chat.FromUserId))
                return;

            if (chat.ChannelId != session.Channel.Id )
            {
                if (ShouldNotifyCrossChannelPost(chat, session))
                {
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                    {
                        if (!session.Usernames.TryGetValue(chat.FromUserId, out var username))
                            username = "Unknown User";
                        var channelName = DI.GetRepository<Channel>().Get(chat.ChannelId).Name.Color(ConsoleColor.Green);
                        var chanNum = ListChannels.GetChannelList(session).IndexOf(x => x.Id == chat.ChannelId) + 1;
                        session.Io.OutputLine($"{Environment.NewLine}Now: {username.Color(ConsoleColor.White)} has posted in {channelName}, use '/ch {chanNum}' to change channels.");
                        Tutor.Execute(session, "Use '/pref' to change this notification and other preferences.");
                        session.ShowPrompt();
                    }
                }
                return;
            }

            // check if chat is in the session.Chats, it probably isn't
            // in-fact it only would be if this session and the poster of the message have both unlocked the archive.
            if (!session.Chats.ContainsKey(chat.Id))
                session.Chats[chat.Id] = chat;

            int lastRead =
                session.LastMsgPointer ??
                session.MsgPointer;

            bool isAtEndOfMessages = true == session.Chats?.Keys?.Count >= 2 && lastRead == session.Chats.Keys[session.Chats.Keys.Count - 2];

            Action action = () =>
            {
                TryBell(session, chat.FromUserId);
                chat.Write(session, ChatWriteFlags.UpdateLastReadMessage | ChatWriteFlags.LiveMessageNotification, GlobalDependencyResolver.Default);
                if (isAtEndOfMessages)
                {
                    SetMessagePointer.Execute(session, chat.Id);
                    session.LastMsgPointer = session.MsgPointer;
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

        private static bool ShouldNotifyCrossChannelPost(Chat chat, BbsSession session)
        {
            if (session.DoNotDisturb)
                return false;

            var xchanmode = session.GetCrossChannelNotificationMode(DI.GetRepository<Metadata>());
            if (xchanmode == CrossChannelNotificationMode.None)
                return false;

            if (!session.User.Access.HasFlag(AccessFlag.Administrator) &&
                !session.User.Access.HasFlag(AccessFlag.GlobalModerator))
            {
                // check: is channel the message came from is invite only
                // and if so does this user have an invite
                var channel = DI.GetRepository<Channel>().Get(chat.ChannelId);
                if (true == channel?.RequiresInvite)
                {
                    var ucFlag = session.UcFlagRepo.Get(new Dictionary<string, object>
                    {
                        {nameof(UserChannelFlag.ChannelId), channel.Id},
                        {nameof(UserChannelFlag.UserId), session.User.Id}
                    })?.FirstOrDefault() ?? new UserChannelFlag
                    {
                        ChannelId = channel.Id,
                        UserId = session.User.Id
                    };
                    if (true != ucFlag?.Flags.HasFlag(UCFlag.Moderator) &&
                        true != ucFlag?.Flags.HasFlag(UCFlag.Invited))
                    {
                        return false;
                    }
                }
            }

            if (xchanmode.HasFlag(CrossChannelNotificationMode.OncePerChannel))
            {
                if (!session.Items.ContainsKey(SessionItem.CrossChannelNotificationReceivedChannels))
                {
                    session.Items[SessionItem.CrossChannelNotificationReceivedChannels] = new List<int>();
                }
                var receivedChanIds = session.Items[SessionItem.CrossChannelNotificationReceivedChannels] as List<int>;
                if (true == receivedChanIds?.Contains(chat.ChannelId))
                    return false;
                receivedChanIds?.Add(chat.ChannelId);
                return true;
            }

            if (!xchanmode.HasFlag(CrossChannelNotificationMode.Any))
            {
                if (xchanmode.HasFlag(CrossChannelNotificationMode.PostMentionsMe) &&
                    chat.Message.ToLower().Contains(session.User.Name.ToLower()))
                {
                    return true;
                }

                if (xchanmode.HasFlag(CrossChannelNotificationMode.PostIsInResponseToMyMessage) &&
                    chat.ResponseToId.HasValue)
                {
                    var re = DI.GetRepository<Chat>().Get(chat.ResponseToId.Value);
                    if (re?.FromUserId == session.User.Id)
                        return true;
                }

                return false;
            }

            return true;
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

                    string msg = $"{Environment.NewLine}{message.User.Name} has {(message.IsLogin ? "logged in" : "logged out")} at {DateTime.UtcNow.AddHours(session.TimeZone):HH:mm}";
                    if (!string.IsNullOrWhiteSpace(message.LogoutMessage))
                        msg += $" saying \"{message.LogoutMessage}\"";

                    if (sessionsForThisUser > 1)
                    {
                        var s = message.IsLogin ? sessionsForThisUser : sessionsForThisUser - 1;
                        msg += $"{Environment.NewLine}{Constants.Spaceholder.Repeat(3)}{message.User.Name} is logged in with {s} session(s).";
                    }

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
                message.SessionId == session.Id ||
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
                session.Io.SetColors(ConsoleColor.Black, ConsoleColor.White);
                session.Io.OutputLine(
                    "Commodore ".Color(ConsoleColor.Green) +
                    "Color ".Color(ConsoleColor.Yellow) +
                    "Mode ".Color(ConsoleColor.Blue) +
                    "Activated.".Color(ConsoleColor.Red));
            }
            else if (emuTest == (char)126)
            {
                session.Io = new Atascii(session);
                session.Cols = 40;
                session.Io.OutputLine($"{session.Io.NewLine.Repeat(2)}{Constants.Inverser}Atascii{Constants.Inverser} Mode Activated.");
            }
            else
            {
                if ('Y' == emuTest ||
                    'y' == emuTest ||
                    ANSI.TryAutoDetect(session))
                {
                    session.Io = new ANSI(session);
                    session.Cols = 80;
                    session.Io.SetColors(ConsoleColor.Black, ConsoleColor.White);
                    session.Io.OutputLine(
                        "Turning on ".Color(ConsoleColor.White) +
                        "A".Color(ConsoleColor.Green) + "N".Color(ConsoleColor.Yellow) +
                        "S".Color(ConsoleColor.Blue) + "I ".Color(ConsoleColor.Red) +
                        "color.");
                }
            }

            int retries = 6;
            retryLogin:
            retries--;
            if (retries <= 0)
            {
                session.Io.OutputLine("Well, goodbye then!");
                return;
            }
            
            session.Io.Output("who are you?: ".Color(ConsoleColor.Green));
            string username = session.Io.InputLine();
            session.Io.OutputLine();
            var newUser = false;
            if ("new".Equals(username, StringComparison.CurrentCultureIgnoreCase))
            {
                session.Io.Output($"Welcome new user!{Environment.NewLine}What name do you want to go by?: ");
                username = session.Io.InputLine();
                session.Io.OutputLine();
                newUser = true;
            }
            else if ("help".Equals(username, StringComparison.CurrentCultureIgnoreCase))
            {
                session.Io.OutputLineSlow("Help not available\r\n\r\n--- connection terminated ---");
                return;
            }
            else if ("help games".Equals(username, StringComparison.CurrentCultureIgnoreCase))
            {
                session.Io.OutputLineSlow("'games' refers to models, simulations and games which have tactical and strategic applications.\r\n\r\n--- connection terminated ---");
                return;
            }
            else if ("list games".Equals(username, StringComparison.CurrentCultureIgnoreCase))
            {
                session.Io.OutputLineSlow("falken's maze\r\nblack jack\r\ngin rummy\r\nhearts\r\nbridge\r\ncheckers\r\nchess\r\npoker\r\nfighter combat\r\nguerrilla engagement\r\ndesert warfare\r\nair-to-ground actions\r\ntheaterwide tactical warfare\r\ntheaterwide biotoxic and chemical warfare\r\n\r\nglobal thermonuclear war\r\n\r\n--- connection terminated ---");
                return;
            }
            else if ("joshua".Equals(username, StringComparison.CurrentCultureIgnoreCase))
            {
                session.Io.OutputLineSlow("greetings professor falken.\r\n\r\n");
                Thread.Sleep(5000);
                session.Io.OutputLineSlow("--- connection terminated ---");
                return;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                retries = 0;
                goto retryLogin;
            }

            username = username.Trim().ToUpperFirst();
            if (username.Any(c => !char.IsLetter(c)))
            {
                session.Io.OutputLine("Your username must include only letters.");
                goto retryLogin;
            }
            
            if ("Guest".Equals(username, StringComparison.CurrentCultureIgnoreCase))
            {
                session.Io.Error("Guest logins not allowed.  All you need to register is to specify a username and a password, I'm not going to ask for your phone number, address, email address, who referred you, you social security number, your favorite flavor of ice cream, etc... We just need to make an account for you to log in with and don't really care about anything personal.");
                goto retryLogin;
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
                    goto retryLogin;
                }

                if (!newUser && session.Io.Ask("I've never seen you before, you new here?") != 'Y')
                    goto retryLogin;
                
                user = RegisterNewUser(session, username, userRepo);
                session.CurrentLocation = Module.Login;
                if (user == null)
                    return;
                var k = session.Io.Ask("Do you want to read the new user documentation now?");
                if (k == 'Y')
                    ReadFile.Execute(session, Constants.Files.NewUser);
                else
                    session.Io.OutputLine("Once you get logged in, type '/newuser' to read the new user documentation.  It can be very helpful for new users as this system works differently than most.");

                session.User = user;
            }
            else
            {
                session.Io.OutputLine("oh yeah?  prove it!");
                session.Io.Output("password: ");
                InputHandlingFlag inputHandlingFlag = retries > 2 ? InputHandlingFlag.PasswordInput : InputHandlingFlag.None;
                string pw = session.Io.InputLine(inputHandlingFlag)?.ToLower();
                if (!DI.Get<IHasher>().VerifyHash(pw, user.PasswordHash) && !TryUseResetCode(session, user, pw))
                {
                    session.Io.OutputLine("I don't think so.");
                    if (retries <= 1)
                    {
                        ForgotPassword(session, user);
                    }
                    goto retryLogin;
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

            //if (user.Access.HasFlag(AccessFlag.Administrator))
            //{
            //    WhoIsOn.Execute(session);
            //    var k = session.Io.Ask("Admin login option: (N)ormal, (S)ilent, (I)nvisible");
            //    switch (k)
            //    {
            //        case 'S':
            //            session.ControlFlags |= SessionControlFlags.DoNotSendNotifications;
            //            break;
            //        case 'I':
            //            session.ControlFlags |= SessionControlFlags.Invisible | SessionControlFlags.DoNotSendNotifications;
            //            break;
            //    }
            //}

            _logger.Log(session, $"{session.User?.Name} has logged in", LoggingOptions.ToDatabase | LoggingOptions.WriteImmedately);
            session.Messager.Publish(session, new UserLoginOrOutMessage(session, true));
        }

        private static bool TryUseResetCode(BbsSession session, User user, string code)
        {
            var di = GlobalDependencyResolver.Default;
            var metaRepo = di.GetRepository<Metadata>();
            var resetCodeMeta = metaRepo.Get(new Dictionary<string, object>
            {
                {nameof(Metadata.UserId), user.Id},
                {nameof(Metadata.Type), MetadataType.PasswordResetCode}
            }).PruneAllButMostRecent(di);

            var hasher = di.Get<IHasher>();

            if (resetCodeMeta == null ||
                string.IsNullOrWhiteSpace(resetCodeMeta.Data) ||
                !hasher.VerifyHash(code.Trim().ToUpper(), resetCodeMeta.Data) ||
                !resetCodeMeta.Data.Equals(code.Trim(), StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            session.Io.OutputLine($"Welcome back, {user.Name}!");
            try
            {
                session.User = user;
                var attempts = 5;
                while (attempts > 0 && !UpdatePassword.Execute(session, false))
                {
                    attempts--;
                }
                metaRepo.Delete(resetCodeMeta);
            }
            finally
            {
                session.User = null;
            }
            return true;
        }

        private static void ForgotPassword(BbsSession session, User user)
        {
            var answer = session.Io.Ask("Forgot your password eh?");
            if (answer != 'Y')
            {
                session.Io.OutputLine("Okay, if you say so!");
                return;
            }

            if (string.IsNullOrWhiteSpace(user.InternetEmail))
            {
                session.Io.OutputLine("Well, you didn't give me your internet e-mail address so ... too bad!");
                return;
            }

            var di = GlobalDependencyResolver.Default;
            var metaRepo = di.GetRepository<Metadata>();
            var resetCodeMeta = metaRepo.Get(new Dictionary<string, object>
            {
                {nameof(Metadata.UserId), user.Id},
                {nameof(Metadata.Type), MetadataType.PasswordResetCode}
            }).PruneAllButMostRecent(di);

            if (resetCodeMeta != null && !string.IsNullOrWhiteSpace(resetCodeMeta.Data))
            {
                session.Io.OutputLine("I've already told the sysop to email you, you'll just have to wait until he gets around to it.");
            }
            else
            {
                var code = Guid.NewGuid().ToString()
                    .Replace("-", "")
                    .Substring(0, 7)
                    .ToUpper();

                metaRepo.Insert(new Metadata
                {
                    UserId = user.Id,
                    Type = MetadataType.PasswordResetCode,
                    Data = code,
                    DateAddedUtc = DateTime.Now,
                });
                
                var msg = $"The user {user.Name} has forgotten their password.  Please e-mail them the reset code '{code}' at '{user.InternetEmail}'.";
                try
                {
                    session.User = user;
                    Commands.Mail.SysopFeedback(session, "PW Reset", msg);
                    session.Io.OutputLine("Okay I'll have the sysop email you.  It might take up to a day and be sure to check your spam folder!");
                }
                finally
                {
                    session.User = null;
                }
            }
        }

        private static User RegisterNewUser(BbsSession session, string username, IRepository<User> userRepo)
        {
            session.CurrentLocation = Module.NewUserRegistration;

            var attempts = 5;
            string newPasswordHash = null;
            while (attempts > 0 && !UpdatePassword.GetNewPasswordHash(session, false, out newPasswordHash))
            {
                attempts--;
            }
            
            if (string.IsNullOrWhiteSpace(newPasswordHash))
            {
                throw new ForceLogoutException("Forced logout due to new user not able to set their password.");
            }

            var now = DateTime.UtcNow;

            User user = new User
            {
                Name = username,
                PasswordHash = newPasswordHash,
                DateAddedUtc = now,
                LastLogonUtc = now,
                TotalLogons = 1,
                Access = AccessFlag.MayLogon
            };

            session.Io.OutputLine(
                "For only the purposes of resetting your password, should you forget it, you *may* supply your Internet E-Mail address.  " +
                "This is not required and you can always set/unset/change it once logged in.  ");

            if ('Y' == session.Io.Ask("Do you want to give your Internet E-Mail address"))
            {
                session.Io.Output("Enter your E-Mail Address: ");
                var email = session.Io.InputLine()?.Trim();
                if (!string.IsNullOrWhiteSpace(email))
                    user.InternetEmail = email;
            }

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
                case "/bookmark":
                case "/bookmarks":
                    if (!BookmarkManager.CheckBookmarkedRead(session))
                        session.Io.Error("You have no saved bookmarks.");
                    return;
                case "/bbs":
                case "/bbslist":
                    BbsList.Execute(session);
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
                    if (session.User.Access.HasFlag(AccessFlag.Administrator) && 'M' == session.Io.Ask("M)essage base view for chats or B)ulletin boards?"))
                        Msg.Execute(session);
                    else
                        Bulletins.Execute(session);
                    return;
                case "/movemsg":
                    MoveMsg.Execute(session, false, parts.Skip(1).ToArray());
                    return;
                case "/movethread":
                    MoveMsg.Execute(session, true, parts.Skip(1).FirstOrDefault());
                    return;
                case "/b":
                case "/bull":
                case "/bulletin":
                case "/bulletins":
                    Bulletins.Execute(session);
                    return;
                case "/o":
                case "/off":
                case "/g":
                case "/logoff":
                case "/quit":
                case "/q":
                    if (Logout.Execute(session, parts[0], string.Join(" ", parts.Skip(1))))
                        CompleteLogout(session);
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
                    UpdatePassword.Execute(session, true);
                    return;
                case "/main":
                case "/menu":
                case "/fauxmain":
                case "/fauxmenu":
                case "/fakemain":
                case "/fakemenu":
                    if (!Commands.MainMenu.Execute(session))
                        CompleteLogout(session);
                    return;
                case "/ignore":
                    Ignore.Execute(session, parts.Skip(1).ToArray());
                    return;
                case "/term":
                case "/setup":
                case "/emu":
                    TermSetup.Execute(session, session.Io.EmulationType);
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
                case "/not":
                case "/nots":
                case "/notifications":
                case "/notification":
                    ShowLoginNotifications(session, (LoginStartupMode)session.Items[SessionItem.StartupMode]);
                    return;
                case "/help":
                case "/?":
                case "?":
                    ExecuteMenu(session, parts.Length >= 2 ? parts[1] : null);
                    return;
                //case "/??":
                //    CommandList.Execute(session);
                //    return;
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
                    EditMessage.Execute(session, string.Join(" ", parts.Skip(1)), useLineEditor: "/edit".Equals(command, StringComparison.CurrentCultureIgnoreCase));
                    return;
                case "/p":
                case "/post":
                    {
                        var _c = session.Io.Ask("(N)ew Message, (R)esponse to last message, (Q)uit");
                        PostChatFlags _flags;
                        if (_c == 'N')
                            _flags = PostChatFlags.IsNewTopic;
                        else if (_c == 'R')
                            _flags = PostChatFlags.None;
                        else
                            return;
                        Msg.PostMessage(session, _flags);
                    }
                    return;
                case "/rere":
                    EditMessage.ReassignReNumber(session, parts.Skip(1).ToArray());
                    return;
                case "/combine":
                    EditMessage.CombineMessages(session, parts.Skip(1).ToArray());
                    return;
                case "/pin":
                    Pin.Execute(session, parts.Skip(1).ToArray());
                    return;
                case "/pins":
                    Pin.ShowPins(session);
                    return;
                case "/unpin":
                    Pin.Unpin(session, parts.Skip(1).ToArray());
                    return;
                case "/dnd":
                    session.DoNotDisturb = !session.DoNotDisturb;
                    return;
                case "/e":
                case "/end":
                case "/last":
                    SetMessagePointer.Execute(session, session.Chats.Keys.Max());
                    session.Chats[session.MsgPointer].Write(session, ChatWriteFlags.UpdateLastMessagePointer | ChatWriteFlags.UpdateLastReadMessage, GlobalDependencyResolver.Default);
                    return;
                case "/first":
                    command = "/0";
                    break; // don't return here
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
                case "/null":
                case "/nullspace":
                    NullSpace.Enter(session);
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
                    ContinuousRead.Execute(session, string.Join(" ", parts.Skip(1)), false);
                    return;
                case "/nonstop":
                    ContinuousRead.Execute(session, string.Join(" ", parts.Skip(1)), true);
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
                case "/ghost":
                case "/ghosts":
                    SessionInfo.Ghosts(session);
                    return;
                case "/shh":
                    if (session.ControlFlags.HasFlag(SessionControlFlags.DoNotSendNotifications))
                    {
                        session.ControlFlags &= ~SessionControlFlags.DoNotSendNotifications;
                        session.Io.OutputLine("Sending multi-node notifications as normal".Color(ConsoleColor.Blue));
                    }
                    else
                    {
                        session.ControlFlags |= SessionControlFlags.DoNotSendNotifications;
                        session.Io.OutputLine("Suspending multi-node notifications".Color(ConsoleColor.Blue));
                    }
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
                case "/b64":
                    session.Io.OutputLine(B64.EncodeOrDecode(string.Join(" ", parts.Skip(1))).Color(ConsoleColor.Blue));
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
                        if (parts.Length >= 2 && double.TryParse(parts[1], out double i) && i >= 0.25)
                            session.StartPingPong(i, silently: false);
                        else
                            session.StartPingPong(0, silently: false);
                    }
                    return;
                case "/ss":
                    if (session.PingType != PingPongType.ScreenSaver)
                    {
                        session.PingType = PingPongType.ScreenSaver;
                        session.Io.Error("Screen saver enabled");
                    }
                    else
                    {
                        session.PingType = PingPongType.Invisible;
                        session.Io.Error("Screen saver disabled");
                    }
                    return;
                case "/ssreplay":
                    if (session.PingType != PingPongType.Replay)
                    {
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int i))
                            session.ReplayNum = i;
                        else
                            session.ReplayNum = 0;
                        session.PingType = PingPongType.Replay;
                        session.Io.Error("Replay mode enabled");
                        session.OnPingPong = () =>
                        {
                            Replay(session);
                            session.ShowPrompt();
                        };
                        Replay(session);
                    }
                    else
                    {
                        session.PingType = PingPongType.Invisible;
                        session.Io.Error("Replay mode disabled");
                        session.OnPingPong = () => 
                        {
                            if (session.PingType == PingPongType.Full)
                                session.ShowPrompt();
                        };
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
                case "/rm":
                case "/mr":
                case "/mailread":
                case "/readmail":
                    Commands.Mail.ReadLatest(session);
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
                case "/blurbs":
                    Blurbs.BlurbAdmin(session, "list");
                    return;
                case "/banner":
                    Banners.Show(session, parts.Skip(1).FirstOrDefault());
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
                        var bbsUptime = DateTime.UtcNow - SysopScreen.StartedAtUtc;
                        session.Io.OutputLine($"{Constants.BbsName} Uptime: {bbsUptime.Days}d {bbsUptime.Hours}h {bbsUptime.Minutes}m");
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
                case "/gopher":
                    Gopher.Execute(session);
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
                case "/mark":
                    MarkChats.Execute(session, parts.Skip(1).ToArray());
                    return;
                case "/ur":
                    SetMessagePointer.SetToFirstUnreadMessage(session);
                    return;
                case "/since":
                    SetMessagePointer.SetToDate(session, string.Join(" ", parts.Skip(1)));
                    return;
                case "/debug":
                    Services.GlobalCommands.Debug.Execute(session, parts.Skip(1).ToArray());
                    return;
                case "/archive":
                case "/archived":
                    SwitchOrMakeChannel.ToggleArchive(session);
                    return;
                case ",":
                case "<":
                    SetMessagePointer.Execute(session, session.MsgPointer - 1, reverse: true);
                    session.Chats[session.MsgPointer].Write(session, ChatWriteFlags.UpdateLastMessagePointer | ChatWriteFlags.UpdateLastReadMessage, GlobalDependencyResolver.Default);
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
                        int nextChannelNumber = currentChannelNumber.Value - 1;
                        if (nextChannelNumber < 0)
                            nextChannelNumber = chans.Count - 1;
                        //var nextChannelId = chans.ItemKey(nextChannelNumber.Value) ?? chans.Last().Key;
                        //nextChannelNumber = chans.ItemNumber(nextChannelId);
                        SwitchOrMakeChannel.Execute(session, $"{nextChannelNumber+1}", false);
                    }
                    return;
                case "]":
                case "}":
                    {
                        var chans = new SortedList<int, Channel>(GetChannel.GetChannels(session)
                            .ToDictionary(k => k.Id));
                        var currentChannelNumber = chans.ItemNumber(session.Channel.Id);
                        int nextChannelNumber = currentChannelNumber.Value + 1;
                        if (nextChannelNumber >= chans.Count)
                            nextChannelNumber = 0; 
                        //var nextChannelId = chans.ItemKey(nextChannelNumber.Value) ?? chans.First().Key;
                        //nextChannelNumber = chans.ItemNumber(nextChannelId);
                        SwitchOrMakeChannel.Execute(session, $"{nextChannelNumber+1}", false);
                    }
                    return;
            }

            if (command.Length > 1 && int.TryParse(command.Substring(1), out int msgNum))
            {
                if (msgNum == 0)
                {
                    if (!session.Items.ContainsKey(SessionItem.ShowChatArchive) ||
                        (bool)session.Items[SessionItem.ShowChatArchive] != true)
                    {
                        session.Io.Error("There may be earlier messages, use '" + "/archive".Color(ConsoleColor.Yellow) + "' to unlock the archive!");
                    }
                }
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
                    case "ren":
                        RenameChannel.Execute(session);
                        break;
                    case "0":
                        NullSpace.Enter(session);
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
                case "more":
                    More.Show(session);
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
                case "misc":
                    Menus.MiscMenu.Show(session);
                    break;
                default:
                    Menus.MainMenu.Show(session);
                    break;
            }
        }

        private static void Replay(BbsSession session)
        {
            if (true != session?.Chats?.Keys?.Any())
                return;

            int next = session.ReplayNum;
            if (next < 0 || next >= session.Chats.Keys.Count)
                next = 0;
            var key = session.Chats.ItemKey(next);
            if (!key.HasValue)
                return;
            next++;
            session.ReplayNum = next;

            var chat = session.Chats[key.Value];
            if (!session.Usernames.ContainsKey(chat.FromUserId))
            {
                string un = session.UserRepo.Get(chat.FromUserId)?.Name;
                if (!string.IsNullOrWhiteSpace(un))
                    session.Usernames[chat.FromUserId] = un;
            }

            var flags = ChatWriteFlags.None;
            var line = chat.GetWriteString(session, flags);
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Green))
            {
                line = $"{Environment.NewLine}{Constants.InlineColorizer}{(int)ConsoleColor.Blue}{Constants.InlineColorizer}Replay: {Constants.InlineColorizer}-1{Constants.InlineColorizer}{line}";
                //session.Io.OutputLine();
                //session.Io.SetForeground(ConsoleColor.Blue);
                //session.Io.Output("REPLAY: ");

                session.Io.OutputLine(line, OutputHandlingFlag.Nonstop);
            }
        }

        private static void CompleteLogout(BbsSession session)
        {
            var randomBbss = BbsList.GetRandom(5, session.Io.EmulationType);
            if (true == randomBbss?.Any())
            {
                session.Io.OutputLine($"{Constants.Inverser}{"Call these other fine boards!".Color(ConsoleColor.Yellow)}{Constants.Inverser}");
                foreach (var bbs in randomBbss)
                {
                    var port = "";
                    if (!string.IsNullOrWhiteSpace(bbs.Port))
                        port = $":{bbs.Port}";
                    session.Io.OutputLine($"{bbs.Name} {bbs.Address.Color(ConsoleColor.Green)}{port}");
                }
            }

            session.Io.OutputLine("Goodbye!");
            session.Disconnect();
        }
    }
}
