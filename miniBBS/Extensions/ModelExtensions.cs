using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.UserIo;
using System;
using System.Linq;

namespace miniBBS.Extensions
{
    public static class ModelExtensions
    {
        public static void Write(this Chat chat, BbsSession session, bool updateLastReadMessageNumber = true, bool monochrome = false)
        {
            if (!session.Usernames.ContainsKey(chat.FromUserId))
            {
                string un = DI.GetRepository<User>().Get(chat.FromUserId)?.Name;
                if (!string.IsNullOrWhiteSpace(un))
                    session.Usernames[chat.FromUserId] = un;
            }

            string username = session.Usernames.ContainsKey(chat.FromUserId) ? session.Usernames[chat.FromUserId] : $"Unknown (ID:{chat.FromUserId})";
            var chatNum = session.Chats.ItemNumber(chat.Id);
            var reNum = session.Chats.ItemNumber(chat.ResponseToId);

            if (monochrome || session.Io.EmulationType == TerminalEmulation.Ascii)
            {   
                session.Io.OutputLine($"[{chatNum}:{chat.DateUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm}] <{username}> re:{reNum}{Environment.NewLine}{chat.Message}");
            }
            else
            {
                var color = session.Io.GetForeground();
                session.Io.SetForeground(ConsoleColor.Cyan);
                session.Io.Output("[");
                session.Io.SetForeground(ConsoleColor.Gray);
                session.Io.Output(chatNum.ToString());
                session.Io.SetForeground(ConsoleColor.White);
                session.Io.Output(":");
                session.Io.SetForeground(ConsoleColor.Gray);
                session.Io.Output(chat.DateUtc.AddHours(session.TimeZone).ToString("yy-MM-dd HH:mm"));
                session.Io.SetForeground(ConsoleColor.Cyan);
                session.Io.Output("] <");
                session.Io.SetForeground(ConsoleColor.Yellow);
                session.Io.Output(username);
                session.Io.SetForeground(ConsoleColor.Cyan);
                session.Io.Output(">");
                session.Io.SetForeground(ConsoleColor.DarkGray);
                session.Io.OutputLine($" re:{reNum}");
                session.Io.SetForeground(ConsoleColor.Green);
                session.Io.Output(chat.Message);
                session.Io.SetForeground(color);
                session.Io.OutputLine();
            }

            if (updateLastReadMessageNumber)
                session.LastReadMessageNumber = chat.Id;
        }

        public static bool CanJoin(this Channel channel, BbsSession session)
        {
            var userFlags = session.UcFlagRepo.Get(f => f.UserId, session.User.Id)
                .ToDictionary(k => k.ChannelId);

            bool result = !channel.RequiresInvite ||
                session.User.Access.HasFlag(AccessFlag.Administrator) ||
                session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                (userFlags.ContainsKey(channel.Id) && (userFlags[channel.Id].Flags & (UCFlag.Invited | UCFlag.Moderator)) > 0);

            return result;
        }
    }
}
