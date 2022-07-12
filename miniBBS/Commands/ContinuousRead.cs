using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Linq;

namespace miniBBS.Commands
{
    public static class ContinuousRead
    {
        public static void Execute(BbsSession session)
        {
            var chats = DI.GetRepository<Chat>().Get(c => c.ChannelId, session.Channel.Id)
                .Where(c => c.Id >= session.MsgPointer)
                .OrderBy(c => c.Id);

            var lines = chats.Select(c =>
            {
                string username = session.Usernames.ContainsKey(c.FromUserId) ? session.Usernames[c.FromUserId] : $"Unknown (ID:{c.FromUserId})";
                return $"[{session.Chats.ItemNumber(c.Id)}:{c.DateUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm}] <{username}> re:{session.Chats.ItemNumber(c.ResponseToId)}{Environment.NewLine}{c.Message}";
            });

            string all = string.Join(Environment.NewLine, lines);

            session.Io.OutputLine(all);
        }
    }
}
