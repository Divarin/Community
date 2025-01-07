using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Extensions
{
    public static class ModelExtensions
    {
        public static void Write(this Chat chat, BbsSession session, ChatWriteFlags flags, IDependencyResolver di)
        {
            if (chat == null || session == null)
                return;

            if (!flags.HasFlag(ChatWriteFlags.DoNotShowMessage))
            {
                if (!session.Usernames.ContainsKey(chat.FromUserId))
                {
                    string un = session.UserRepo.Get(chat.FromUserId)?.Name;
                    if (!string.IsNullOrWhiteSpace(un))
                        session.Usernames[chat.FromUserId] = un;
                }

                var line = chat.GetWriteString(session, flags);
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Green))
                {
                    session.Io.OutputLine(line);
                }
            }

            if (flags.HasFlag(ChatWriteFlags.UpdateLastReadMessage))
                session.LastReadMessageNumber = chat.Id;
            if (flags.HasFlag(ChatWriteFlags.UpdateLastMessagePointer))
                session.LastMsgPointer = chat.Id;
            
            session.MarkRead(chat.Id, di);
        }

        public static string GetWriteString(this Chat chat, BbsSession session, ChatWriteFlags flags = ChatWriteFlags.None)
        {
            if (chat == null || session == null)
                return string.Empty;

            Func<string> endClr = () =>
                flags.HasFlag(ChatWriteFlags.Monochorome) ?
                string.Empty :
                $"{Constants.InlineColorizer}-1{Constants.InlineColorizer}";

            Func<ConsoleColor, string> clr = _clr =>
                flags.HasFlag(ChatWriteFlags.Monochorome) ?
                string.Empty :
                $"{Constants.InlineColorizer}{(int)_clr}{Constants.InlineColorizer}";

            string username = session.Usernames.ContainsKey(chat.FromUserId) ? session.Usernames[chat.FromUserId] : $"Unknown (ID:{chat.FromUserId})";
            var chatNum = session.Chats.ItemNumber(chat.Id);
            var reNum = session.Chats.ItemNumber(chat.ResponseToId)?.ToString() ?? "none";
            if (reNum == "none" && chat.ResponseToId.HasValue)
                reNum = "archived";

            string line;

            if (flags.HasFlag(ChatWriteFlags.FormatForMessageBase))
            {
                line = string.Join("", new[]
                {
                    "Msg #: ".Color(ConsoleColor.Cyan),
                    chatNum.ToString().PadRight(12).Color(ConsoleColor.White),
                    "Re   : ".Color(ConsoleColor.Cyan),
                    reNum.Color(ConsoleColor.DarkGray),
                    Environment.NewLine,
                    "From : ".Color(ConsoleColor.Cyan),
                    username.PadRight(12).Color(ConsoleColor.Yellow),
                    "Date : ".Color(ConsoleColor.Cyan),
                    $"{chat.DateUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm}".Color(ConsoleColor.Blue),
                    Environment.NewLine,
                    $"{Constants.Spaceholder}---------- ",
                    Environment.NewLine,
                    Quote(session, chat.ResponseToId),
                    chat.Message.Color(ConsoleColor.Green)
                });
            }
            else
            {
                var date = chat.DateUtc.AddHours(session.TimeZone);
                var headerFormat = session.Items.ContainsKey(SessionItem.ChatHeaderFormat) ? session.Items[SessionItem.ChatHeaderFormat] as string : Constants.DefaultChatHeaderFormat;
                var header = headerFormat
                    .Replace("%mn%", chatNum?.ToString().Color(ConsoleColor.White))
                    .Replace("%re%", reNum.Color(ConsoleColor.DarkGray))
                    .Replace("%yy%", $"{date:yyyy}".Color(ConsoleColor.Blue))
                    .Replace("%y%", $"{date:yy}".Color(ConsoleColor.Blue))
                    .Replace("%mm%", $"{date:MM}".Color(ConsoleColor.Blue))
                    .Replace("%m%", $"{date.Month}".Color(ConsoleColor.Blue))
                    .Replace("%dd%", $"{date:dd}".Color(ConsoleColor.Blue))
                    .Replace("%d%", $"{date.Day}".Color(ConsoleColor.Blue))
                    .Replace("%hh%", $"{date:HH}".Color(ConsoleColor.Blue))
                    .Replace("%h%", $"{date.Hour}".Color(ConsoleColor.Blue))
                    .Replace("%min%", $"{date:mm}".Color(ConsoleColor.Blue))
                    .Replace("%un%", username.Color(ConsoleColor.Yellow))
                    .Replace("[", "[".Color(ConsoleColor.Cyan))
                    .Replace("]", "]".Color(ConsoleColor.Cyan))
                    .Replace("<", "<".Color(ConsoleColor.Cyan))
                    .Replace(">", ">".Color(ConsoleColor.Cyan))
                    .Replace("(re:", "(re:".Color(ConsoleColor.DarkGray))
                    .Replace(")", ")".Color(ConsoleColor.DarkGray))
                    .Replace("%nl%", Environment.NewLine);

                if (!header.EndsWith(Environment.NewLine))
                    header += " ";

                line = header + chat.Message.Color(ConsoleColor.Green);
                //line = string.Join("", new[]
                //{
                //    $"{clr(ConsoleColor.Cyan)}[{clr(ConsoleColor.White)}{chatNum}",
                //    $"{clr(ConsoleColor.DarkGray)} (re:{reNum}) ",
                //    $"{clr(ConsoleColor.White)}:",
                //    $"{clr(ConsoleColor.Blue)}{chat.DateUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm}",
                //    $"{clr(ConsoleColor.Cyan)}] <{clr(ConsoleColor.Yellow)}{username}{clr(ConsoleColor.Cyan)}>",
                //    $"{clr(ConsoleColor.Green)}{chat.Message}{endClr()}"
                //});
            }
            
            if (flags.HasFlag(ChatWriteFlags.LiveMessageNotification))
                line = $"{Environment.NewLine}Now: ".Color(ConsoleColor.Blue) + line;

            return line;
        }

        private static string Quote(BbsSession session, int? responseToId)
        {
            if (!responseToId.HasValue || true != session?.Chats?.ContainsKey(responseToId.Value))
                return string.Empty;

            var quoted = session.Chats[responseToId.Value];
            string un = session.Usernames.ContainsKey(quoted.FromUserId) ? session.Usernames[quoted.FromUserId] : "Unknown";
            var result = 
                $"{Constants.Spaceholder}--- Begin Quote from {un}, msg #{session.Chats.ItemNumber(quoted.Id)} --- ".Color(ConsoleColor.DarkGray) +
                Environment.NewLine +
                quoted.Message.Color(ConsoleColor.Blue) +
                Environment.NewLine +
                $"{Constants.Spaceholder}--- End Quote --- ".Color(ConsoleColor.DarkGray) +
                Environment.NewLine;

            return result;
        }

        public static bool CanJoin(this Channel channel, BbsSession session)
        {
            if (channel == null || session == null)
                return false;

            var userFlags = session.UcFlagRepo.Get(f => f.UserId, session.User.Id)
                .ToDictionary(k => k.ChannelId);

            bool result = !channel.RequiresInvite ||
                session.User.Access.HasFlag(AccessFlag.Administrator) ||
                session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                (userFlags.ContainsKey(channel.Id) && (userFlags[channel.Id].Flags & (UCFlag.Invited | UCFlag.Moderator)) > 0);

            return result;
        }

        public static IEnumerable<User> GetModerators(this Channel channel, BbsSession session, bool includeAdminsAndGlobalMods)
        {
            if (channel == null || session == null)
                return new User[] { };

            List<User> users = new List<User>();

            if (includeAdminsAndGlobalMods)
            {
                var f = AccessFlag.Administrator | AccessFlag.GlobalModerator;
                users.AddRange(session.UserRepo.Get().Where(u => (u.Access & f) > 0));
            }

            var moderators = session.UcFlagRepo
                .Get(f => f.ChannelId, channel.Id)
                ?.Where(f => f.Flags.HasFlag(UCFlag.Moderator))
                ?.Select(f => f.UserId)
                ?.Distinct()
                ?.Select(id => session.UserRepo.Get(id));

            if (true == moderators?.Any())
                users.AddRange(moderators);

            return users;
        }

        public static LoginStartupMode GetStartupMode(this User user, IRepository<Metadata> metaRepo)
        {
            if (user == null || metaRepo == null)
                return LoginStartupMode.MainMenu;

            var meta = metaRepo.Get(new Dictionary<string, object>
            {
                {nameof(Metadata.UserId), user.Id},
                {nameof(Metadata.Type), MetadataType.LoginStartupMode}
            })?.FirstOrDefault();

            LoginStartupMode mode = LoginStartupMode.MainMenu;
            if (!string.IsNullOrWhiteSpace(meta?.Data) && Enum.TryParse(meta.Data, out LoginStartupMode lsm))
                mode = lsm;

            return mode;
        }

        public static bool TryGetDataAs<T>(this Metadata meta, out T value)
        {
            if (meta?.Data == null)
            {
                value = default;
                return false;
            }

            try
            {
                value = JsonConvert.DeserializeObject<T>(meta.Data);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }
    }
}
