using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace miniBBS.Commands
{
    public static class UserPreferences
    {
        public static void Execute(BbsSession session)
        {
            var metaRepo = DI.GetRepository<Metadata>();
            bool exitLoop = false;
            do
            {
                var lsmMeta = GetMeta(session, metaRepo, MetadataType.LoginStartupMode);
                
                LoginStartupMode mode = LoginStartupMode.MainMenu;
                if (!string.IsNullOrWhiteSpace(lsmMeta?.Data) && Enum.TryParse(lsmMeta.Data, out LoginStartupMode lsm))
                    mode = lsm;

                string headerFormat = Constants.DefaultChatHeaderFormat;
                var headerFormatMeta = GetMeta(session, metaRepo, MetadataType.ChatHeaderFormat);
                if (!string.IsNullOrWhiteSpace(headerFormatMeta?.Data))
                    headerFormat = headerFormatMeta.Data;

                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
                {
                    session.Io.OutputLine("1) Terminal Setup");
                    session.Io.OutputLine($"2) Login Startup Mode: {mode.FriendlyName()}");
                    session.Io.OutputLine("3) Chat Header Format");
                    session.Io.OutputLine("4) Cross-Channel Notifications");
                    session.Io.OutputLine("Q) Quit");
                }

                session.Io.Output($"{Constants.Inverser}[PREFS] :{Constants.Inverser} ".Color(ConsoleColor.White));
                var inp = session.Io.InputKey();
                session.Io.OutputLine();
                switch (inp)
                {
                    case '1':
                        TermSetup.Execute(session);
                        break;
                    case '2':
                        mode = mode == LoginStartupMode.ChatRooms ? LoginStartupMode.MainMenu : LoginStartupMode.ChatRooms;
                        lsmMeta.Data = mode.ToString();
                        metaRepo.Update(lsmMeta);
                        break;
                    case '3':
                        headerFormat = GetNewMessageHeaderFormat(session, headerFormat);

                        if (headerFormatMeta == null)
                            headerFormatMeta = new Metadata()
                            {
                                UserId = session.User.Id,
                                Type = MetadataType.ChatHeaderFormat
                            };
                        headerFormatMeta.Data = headerFormat;
                        metaRepo.InsertOrUpdate(headerFormatMeta);
                        session.Items[SessionItem.ChatHeaderFormat] = headerFormat;
                        break;
                    case '4':
                        SetCrossChannelNotifications(session, metaRepo);
                        break;
                    default:
                        exitLoop = true;
                        break;
                }
            } while (!exitLoop);
        }

        private static void SetCrossChannelNotifications(BbsSession session, IRepository<Metadata> metaRepo)
        {
            var xChanMeta = GetMeta(session, metaRepo, MetadataType.CrossChannelNotifications);
            if (string.IsNullOrWhiteSpace(xChanMeta?.Data) || !Enum.TryParse(xChanMeta.Data, out CrossChannelNotificationMode xChanMode))
                xChanMode = Constants.DefaultCrossChannelNotificationMode;

            if (xChanMeta == null)
            {
                xChanMeta = new Metadata
                {
                    UserId = session.User.Id,
                    DateAddedUtc = DateTime.UtcNow,
                    Type = MetadataType.CrossChannelNotifications,
                    Data = Constants.DefaultCrossChannelNotificationMode.ToString()
                };
            }

            var exitLoop = false;

            Action<CrossChannelNotificationMode> Toggle = m =>
            {
                if (xChanMode.HasFlag(m))
                    xChanMode &= ~m;
                else
                    xChanMode |= m;
            };

            Func<int, string, bool, string> FormatLine = (lineNum, str, cnd) =>
            {
                var tf = cnd ? "TRUE ".Color(ConsoleColor.Green) : "FALSE".Color(ConsoleColor.Red);
                return $"{lineNum}) {tf} {str}";
            };

            do
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
                {
                    var builder = new StringBuilder();
                    builder.AppendLine("Notifications about new posts in other channels while you are online:".Color(ConsoleColor.Cyan));
                    builder.AppendLine(FormatLine(1, "None", xChanMode == 0));
                    builder.AppendLine(FormatLine(2, "Is RE: one of your posts", xChanMode.HasFlag(CrossChannelNotificationMode.PostIsInResponseToMyMessage)));
                    builder.AppendLine(FormatLine(3, "Mentions you", xChanMode.HasFlag(CrossChannelNotificationMode.PostMentionsMe)));
                    builder.AppendLine(FormatLine(4, "Any posts", xChanMode.HasFlag(CrossChannelNotificationMode.Any)));
                    builder.AppendLine(FormatLine(5, "Only 1 per channel", xChanMode.HasFlag(CrossChannelNotificationMode.OncePerChannel)));
                    builder.AppendLine($"D) Revert to default");
                    builder.AppendLine($"Q) Quit");
                    session.Io.Output(builder.ToString());

                    var k = session.Io.Ask("Cross-Channel Notifications".Color(ConsoleColor.Yellow));
                    switch (k)
                    {
                        case '1':
                            session.Io.OutputLine("If 'None' then never receive a notification about new posts in other channels while you are online.".Color(ConsoleColor.DarkGray));
                            xChanMode = 0; 
                            break;
                        case '2':
                            session.Io.OutputLine("If the new post is in response to a one of your posts.".Color(ConsoleColor.DarkGray));
                            Toggle(CrossChannelNotificationMode.PostIsInResponseToMyMessage); 
                            break;
                        case '3':
                            session.Io.OutputLine("If the new post contains your username.".Color(ConsoleColor.DarkGray));
                            Toggle(CrossChannelNotificationMode.PostMentionsMe);
                            break;
                        case '4':
                            session.Io.OutputLine("Any posts regardless of #2 or #3.".Color(ConsoleColor.DarkGray));
                            Toggle(CrossChannelNotificationMode.Any);
                            break;
                        case '5':
                            session.Io.OutputLine("Will only receive one notification even if multiple posts are made.  This resets if you go to that channel and then leave it.".Color(ConsoleColor.DarkGray));
                            Toggle(CrossChannelNotificationMode.OncePerChannel);
                            break;
                        case 'd': case 'D': xChanMode = Constants.DefaultCrossChannelNotificationMode; break;
                        case 'q': case 'Q': exitLoop = true; break;
                    }
                }
            } while (!exitLoop);

            session.Items[SessionItem.CrossChannelNotificationMode] = xChanMode;
            xChanMeta.Data = xChanMode.ToString();
            metaRepo.Update(xChanMeta);
        }

        private static string GetNewMessageHeaderFormat(BbsSession session, string headerFormat)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
            {
                var builder = new StringBuilder();
                builder.AppendLine($"Current Format: {(Constants.DefaultChatHeaderFormat.Equals(headerFormat, StringComparison.CurrentCultureIgnoreCase) ? "Default" : headerFormat)}");
                builder.AppendLine("Example:");
                builder.AppendLine(ShowHeaderFormatExample(session, headerFormat));
                builder.AppendLine("1) Revert to default");
                builder.AppendLine($"2) Define custom: {(Constants.DefaultChatHeaderFormat.Equals(headerFormat, StringComparison.CurrentCultureIgnoreCase) ? "" : headerFormat)}");
                
                builder.AppendLine("Q) Quit");
                session.Io.Output(builder.ToString());

                var k = session.Io.Ask("Format");
                switch (k)
                {
                    case '1':
                        headerFormat = Constants.DefaultChatHeaderFormat;
                        break;
                    case '2':
                        {
                            ListCustomeHeaderVariableNames(session);
                            session.Io.Output("Enter format: ");
                            headerFormat = session.Io.InputLine();
                            if (string.IsNullOrWhiteSpace(headerFormat))
                                headerFormat = Constants.DefaultChatHeaderFormat;
                        }
                        break;
                }
            }

            return headerFormat;
        }

        private static void ListCustomeHeaderVariableNames(BbsSession session)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Variables");
            builder.AppendLine("%mn%".PadRight(7).Color(ConsoleColor.Green) + "Message Number");
            builder.AppendLine("%re%".PadRight(7).Color(ConsoleColor.Green) + "In-Response-To (re:) Number");
            builder.AppendLine("%yy%".PadRight(7).Color(ConsoleColor.Green) + "Date Year, 4 digit");
            builder.AppendLine("%y%".PadRight(7).Color(ConsoleColor.Green) + "Date Year, 2 digit");
            builder.AppendLine("%mm%".PadRight(7).Color(ConsoleColor.Green) + "Date Month, 2 digit");
            builder.AppendLine("%m%".PadRight(7).Color(ConsoleColor.Green) + "Date Month, 1 or 2 digit");
            builder.AppendLine("%dd%".PadRight(7).Color(ConsoleColor.Green) + "Date Day, 2 digit");
            builder.AppendLine("%d%".PadRight(7).Color(ConsoleColor.Green) + "Date Day, 1 or 2 digit");
            builder.AppendLine("%hh%".PadRight(7).Color(ConsoleColor.Green) + "Hour, 2 digit");
            builder.AppendLine("%h%".PadRight(7).Color(ConsoleColor.Green) + "Hour, 1 or 2 digit");
            builder.AppendLine("%min%".PadRight(7).Color(ConsoleColor.Green) + "Minute, always 2 digit");
            builder.AppendLine("%un%".PadRight(7).Color(ConsoleColor.Green) + "Username");
            builder.AppendLine("%nl%".PadRight(7).Color(ConsoleColor.Green) + "Newline");
            session.Io.Output(builder.ToString());
        }

        private static string ShowHeaderFormatExample(BbsSession session, string headerFormat)
        {
            int msgNum = 12345;
            DateTime now = DateTime.UtcNow.AddHours(session.TimeZone);
            int re = msgNum - 1;

            var header = headerFormat
                .Replace("%mn%", msgNum.ToString().Color(ConsoleColor.White))
                .Replace("%re%", re.ToString().Color(ConsoleColor.DarkGray))
                .Replace("%yy%", $"{now:yyyy}".Color(ConsoleColor.Blue))
                .Replace("%y%", $"{now:yy}".Color(ConsoleColor.Blue))
                .Replace("%mm%", $"{now:MM}".Color(ConsoleColor.Blue))
                .Replace("%m%", $"{now.Month}".Color(ConsoleColor.Blue))
                .Replace("%dd%", $"{now:dd}".Color(ConsoleColor.Blue))
                .Replace("%d%", $"{now.Day}".Color(ConsoleColor.Blue))
                .Replace("%hh%", $"{now:HH}".Color(ConsoleColor.Blue))
                .Replace("%h%", $"{now.Hour}".Color(ConsoleColor.Blue))
                .Replace("%min%", $"{now:mm}".Color(ConsoleColor.Blue))
                .Replace("%un%", session.User.Name.Color(ConsoleColor.Yellow))
                .Replace("[", "[".Color(ConsoleColor.Cyan))
                .Replace("]", "]".Color(ConsoleColor.Cyan))
                .Replace("<", "<".Color(ConsoleColor.Cyan))
                .Replace(">", ">".Color(ConsoleColor.Cyan))
                .Replace("(re:", "(re:".Color(ConsoleColor.DarkGray))
                .Replace(")", ")".Color(ConsoleColor.DarkGray))
                .Replace("%nl%", Environment.NewLine);

            if (!header.EndsWith(Environment.NewLine))
                header += " ";

            return header + "This is an example message!".Color(ConsoleColor.Green);
        }

        private static Metadata GetMeta(BbsSession session, IRepository<Metadata> metaRepo, MetadataType metaType)
        {
            var result = metaRepo.Get(new Dictionary<string, object>
                {
                    {nameof(Metadata.UserId), session.User.Id},
                    {nameof(Metadata.Type), metaType}
                })?.PruneAllButMostRecent(metaRepo);
            return result;
        }
    }
}
