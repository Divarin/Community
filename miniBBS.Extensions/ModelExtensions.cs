using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
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
                string un = session.UserRepo.Get(chat.FromUserId)?.Name;
                if (!string.IsNullOrWhiteSpace(un))
                    session.Usernames[chat.FromUserId] = un;
            }

            string username = session.Usernames.ContainsKey(chat.FromUserId) ? session.Usernames[chat.FromUserId] : $"Unknown (ID:{chat.FromUserId})";
            var chatNum = session.Chats.ItemNumber(chat.Id);
            var reNum = session.Chats.ItemNumber(chat.ResponseToId);

            Func<ConsoleColor, string> clr = _clr => 
                monochrome ? 
                string.Empty : 
                $"{Constants.InlineColorizer}{(int)_clr}{Constants.InlineColorizer}";

            Func<string> endClr = () => monochrome ? string.Empty : $"{Constants.InlineColorizer}-1{Constants.InlineColorizer}";

            var line = string.Join("", new[]
            {
                $"{clr(ConsoleColor.Cyan)}[{clr(ConsoleColor.White)}{chatNum}{clr(ConsoleColor.White)}:",
                $"{clr(ConsoleColor.Blue)}{chat.DateUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm}",
                $"{clr(ConsoleColor.Cyan)}] <",
                $"{clr(ConsoleColor.Yellow)}{username}{clr(ConsoleColor.Cyan)}>",
                $"{clr(ConsoleColor.DarkGray)} re:{reNum}{Environment.NewLine}",
                $"{clr(ConsoleColor.Green)}{chat.Message}{endClr()}"
            });

            session.Io.OutputLine(line);

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
