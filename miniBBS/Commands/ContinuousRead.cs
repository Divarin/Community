using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System.Linq;

namespace miniBBS.Commands
{
    public static class ContinuousRead
    {
        public static void Execute(BbsSession session, string args, bool nonstop)
        {
            var startFrom = session.Chats.ItemNumber(session.MsgPointer);
            if (!string.IsNullOrWhiteSpace(args) && int.TryParse(args, out var n))
            {
                bool isRelative = args[0] == '-' || args[0] == '+';
                if (isRelative)
                    startFrom += n;
                else
                    startFrom = n;
            }

            var chatKeys = session.Chats
                .Where(x => session.Chats.ItemNumber(x.Key) >= startFrom);

            if (!string.IsNullOrWhiteSpace(args) && args[0] == '-')
            {
                var upTo = session.LastMsgPointer.HasValue ? session.LastMsgPointer : session.MsgPointer;
                if (upTo >= startFrom)
                {
                    chatKeys = chatKeys
                        .Where(x => x.Key <= upTo);
                }
            }

            var chats = chatKeys
                .OrderBy(x => x.Key)
                .Select(x => x.Value)
                .ToList();

            var lines = chats.Select(c => c.GetWriteString(session));
            string all = string.Join(session.Io.NewLine, lines);

            var flags = nonstop ? OutputHandlingFlag.Nonstop : OutputHandlingFlag.None;
            session.Io.OutputLine(all, flags);
        }
    }
}
