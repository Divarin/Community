using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions_Session;
using miniBBS.Extensions_UserIo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Commands
{
    public static class Ignore
    {
        public static void Execute(BbsSession session, params string[] args)
        {
            if (true != args?.Any())
                ShowIgnoreList(session);
            else
                ToggleIgnore(session, args);
        }

        private static void ShowIgnoreList(BbsSession session)
        {
            var ignoreList = session.IgnoreList();
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                if (true == ignoreList?.Any())
                    session.Io.OutputLine($"Users currently being ignored: {string.Join(", ", ignoreList)}");
                else
                    session.Io.OutputLine("No users currently being ignored.");
            }
        }

        private static void ToggleIgnore(BbsSession session, string[] usernames)
        {
            var ignoreList = session.IgnoreList();
            if (ignoreList == null)
            {
                ignoreList = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
                session.Items[SessionItem.IgnoreList] = ignoreList;
            }

            foreach (var username in usernames)
            {
                if (session.IsIgnoring(username))
                {
                    ignoreList.Remove(username);
                    session.Io.Error($"Removed {username} from your ignore list.");
                }
                else
                {
                    ignoreList.Add(username);
                    session.Io.Error($"Added {username} to your ignore list.");
                }
            }
        }
    }
}
