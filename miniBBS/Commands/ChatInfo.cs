using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Text;

namespace miniBBS.Commands
{
    public static class ChatInfo
    {
        public static void Execute(BbsSession session, string chatNum)
        {
            var admin = session.User.Access.HasFlag(AccessFlag.Administrator);

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Gray))
            {
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
                builder.AppendLine($"Chat # : {c}{(admin ? $" (ID: {key.Value})" : "")}");
                builder.AppendLine($"User   : {username}{(admin ? $" ({chat.FromUserId})" : "")}");
                builder.AppendLine($"Date   : {chat.DateUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm:ss}");
                builder.AppendLine($"Re     : #{session.Chats.ItemNumber(chat.ResponseToId)}{(admin ? $" (ID: {chat.ResponseToId})" : "")}");
                builder.AppendLine($"Chan   : {channel}{(admin ? $" (ID: {chat.ChannelId})" : "")}");
                builder.AppendLine($"Snippet: {chat.Message.MaxLength(Constants.MaxSnippetLength)}");

                session.Io.OutputLine(builder.ToString());
            }
        }
    }
}
