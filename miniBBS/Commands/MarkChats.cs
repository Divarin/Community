using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class MarkChats
    {
        public static void Execute(BbsSession session, params string[] args)
        {
            if (args?.Count() != 2)
            {
                ShowUsage(session);
                return;
            }

            bool asRead = false;
            if (args.Last().Equals("read", StringComparison.CurrentCultureIgnoreCase))
                asRead = true;
            else if (!args.Last().Equals("unread", StringComparison.CurrentCultureIgnoreCase))
            {
                session.Io.Error("Missing keyword 'read' or 'unread'.  Type '/mark' for usage information.");
                return;
            }

            var range = GetRange(session.Chats, args.First().ToLower());
            if (!range.Item1.HasValue || !range.Item2.HasValue)
            {
                session.Io.Error("Unable to parse a range of message numbers from your input, type '/mark' for usage information.");
                return;
            }

            var count = DoMark(session, range.Item1.Value, range.Item2.Value, asRead);
            session.Io.Error($"{count} messages marked as {(asRead ? "read" : "unread")}");
        }

        private static int DoMark(BbsSession session, int start, int end, bool asRead)
        {
            int count = 0;
            var di = DI.Get<IDependencyResolver>();
            for (int i=start; i <= end; i++)
            {
                var id = session.Chats.ItemKey(i);
                if (!id.HasValue) continue;
                session.MarkRead(id.Value, di, asRead);
                count++;
            }
            return count;
        }

        private static Tuple<int?, int?> GetRange(SortedList<int, Chat> chats, string arg)
        {
            int? start = null;
            int? end = null;
            int n, m;

            if ("all".Equals(arg, StringComparison.CurrentCultureIgnoreCase))
            {
                start = chats.ItemNumber(chats.Keys.Min());
                end = chats.ItemNumber(chats.Keys.Max());
            }
            else if (int.TryParse(arg, out n))
            {
                start = end = chats.ItemNumber(n);
            }
            else
            {
                var parts = arg.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts?.Length == 2 && int.TryParse(parts[0], out n) && int.TryParse(parts[1], out m))
                {
                    start = Math.Min(n, m);
                    end = Math.Max(n, m);
                }
                else if (parts?.Length == 1 && int.TryParse(parts[0], out n))
                {
                    start = 0;
                    end = n;
                }
                else
                {
                    parts = arg.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 1 && int.TryParse(parts[0], out n))
                    {
                        start = n;
                        end = chats.ItemNumber(chats.Keys.Max());
                    }
                }                    
            }

            return new Tuple<int?, int?>(start, end);
        }

        private static void ShowUsage(BbsSession session)
        {
            var builder = new StringBuilder();
            builder.AppendLine("MARK - Marks messages as 'read' or 'unread'.");
            builder.AppendLine("/mark all unread - Marks all messages in the current channel as unread.");
            builder.AppendLine("/mark all read - Marks all messages in the current channel as read.");
            builder.AppendLine("/mark n unread - Marks message #n as unread.");
            builder.AppendLine("/mark n read - Marks message #n as read.");
            builder.AppendLine("/mark n-m unread - Marks all messaes between #n and #m (including n and m) as unread.");
            builder.AppendLine("/mark n-m read - Marks all messaes between #n and #m (including n and m) as read.");
            builder.AppendLine("/mark n+ unread - Marks all messaes from #n to the end as unread.");
            builder.AppendLine("/mark n+ read - Marks all messaes from #n to the end as read.");
            builder.AppendLine("/mark n- unread - Marks all messaes from 0 to #n as unread.");
            builder.AppendLine("/mark n- read - Marks all messaes from 0 to #n as read.");
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
                session.Io.Output(builder.ToString());
        }
    }
}
