using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using System;
using System.Linq;

namespace miniBBS.Commands
{
    public static class Whisper
    {
        public static void Execute(BbsSession session, params string[] args)
        {
            if (args == null || args.Length < 2)
                return;
            var toUserName = args[0];
            var toUserId = session.Usernames.FirstOrDefault(x => x.Value.StartsWith(toUserName, StringComparison.CurrentCultureIgnoreCase)).Key;
            if (toUserId < 1)
            {
                session.Io.Error("User not found.");
                return;
            }
            toUserName = session.Usernames[toUserId];
            var sessionsList = DI.Get<ISessionsList>();
            var targetSessions = sessionsList.Sessions.Where(s => toUserId == s.User?.Id).ToList();
            if (true != targetSessions?.Any())
            {
                session.Io.Error($"{toUserName} is not online at this time.");
                return;
            }
            var message = string.Join(" ", args.Skip(1));

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.DarkGray))
            {
                session.Io.OutputLine($"You whisper to {toUserName} : {message}");
                session.Messager.Publish(session, new UserMessage(session.Id, toUserId, $"{session.User.Name} whispers to you : {message}")
                {
                    TextColor = ConsoleColor.DarkGray
                });
            }
        }
    }
}
