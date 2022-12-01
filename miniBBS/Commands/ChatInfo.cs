using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions_Collection;
using System;
using System.Text;

namespace miniBBS.Commands
{
    public static class ChatInfo
    {
        public static void Execute(BbsSession session, string chatNum)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Gray))
            {
                if (!session.User.Access.HasFlag(AccessFlag.Administrator))
                    return;

                int c = -1;
                if (string.IsNullOrWhiteSpace(chatNum) || !int.TryParse(chatNum, out c))
                {
                    var nc = session.Chats.ItemNumber(session.LastReadMessageNumber);
                    if (nc.HasValue)
                        c = nc.Value;
                }

                var key = session.Chats.ItemKey(c);
                if (!key.HasValue || !session.Chats.ContainsKey(key.Value))
                {
                    session.Io.OutputLine($"Unable to determine ID for {c}.");
                }

                var chat = session.Chats[key.Value];
                string username = session.Usernames.ContainsKey(chat.FromUserId) ? session.Usernames[chat.FromUserId] : "Unknown";
                var channel = DI.GetRepository<Channel>().Get(chat.ChannelId)?.Name ?? "Deleted channel";

                StringBuilder builder = new StringBuilder();                
                builder.AppendLine($"Chat # : {c} (ID: {key.Value})");
                builder.AppendLine($"User   : {username} ({chat.FromUserId})");
                builder.AppendLine($"Date   : {chat.DateUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm:ss}");
                builder.AppendLine($"Re     : #{session.Chats.ItemNumber(chat.ResponseToId)} (ID: {chat.ResponseToId})");
                builder.AppendLine($"Chan   : {channel} (ID: {chat.ChannelId})");
                builder.AppendLine($"Message: {chat.Message}");

                session.Io.OutputLine(builder.ToString());
            }
        }
    }
}
