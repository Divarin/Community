using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System.Linq;

namespace miniBBS.Commands
{
    public static class ContinuousRead
    {
        public static void Execute(BbsSession session, string args)
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

            var chats = session.Chats
                .Where(x => session.Chats.ItemNumber(x.Key) >= startFrom)
                .OrderBy(x => x.Key)
                .Select(x => x.Value)
                .ToList();
                
            var lines = chats.Select(c => c.GetWriteString(session));
            string all = string.Join(session.Io.NewLine, lines);

            session.Io.OutputLine(all);
        }
    }
}
