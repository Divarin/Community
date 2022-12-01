using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions_UserIo;
using System;
using System.Linq;

namespace miniBBS.Commands
{
    public static class ReadTextFile
    {
        public static void Execute(BbsSession session, params string[] args)
        {
            var browser = DI.Get<ITextFilesBrowser>();
            bool linkFound = false;
            if (args == null || args.Length < 1)
                linkFound = TryReadFromChat(session, browser);
            
            if (!linkFound)
            {
                if (true != args?.Any())
                {
                    session.Io.Error("No text file or basic program link.");
                    return;
                }

                var basicPrograms = browser.FindBasicPrograms(session)
                    .Select(x => x.Split('|').FirstOrDefault()) // just the path part, not the description
                    .ToList();

                string prog = null;
                if (int.TryParse(args[0], out int n) && n >= 1 && n <= basicPrograms.Count)
                    prog = basicPrograms[n - 1];
                else
                    prog = basicPrograms.FirstOrDefault(p => p.EndsWith($"/{args[0]}.bas", StringComparison.CurrentCultureIgnoreCase));

                if (!string.IsNullOrWhiteSpace(prog))
                    browser.ReadLink(session, $"[{prog}]");
                else
                    session.Io.Error($"File not found: '{args[0]}'");
            }
        }

        private static bool TryReadFromChat(BbsSession session, ITextFilesBrowser browser)
        {
            Chat msg;
            bool linkFound = false;
            if (session.LastReadMessageNumber.HasValue && session.Chats.ContainsKey(session.LastReadMessageNumber.Value))
            {
                msg = session.Chats[session.LastReadMessageNumber.Value];
                linkFound = browser.ReadLink(session, msg.Message);
            }

            if (!linkFound && session.ContextPointer.HasValue && session.Chats.ContainsKey(session.ContextPointer.Value))
            {
                msg = session.Chats[session.ContextPointer.Value];
                linkFound = browser.ReadLink(session, msg.Message);
            }

            return linkFound;
        }
    }
}
