using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions_UserIo;
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
            Send(session, toUserId, string.Join(" ", args.Skip(1)));
        }

        public static void Reply(BbsSession session, params string[] args)
        {
            if (!session.Items.ContainsKey(SessionItem.LastWhisperFromUserId))
            {
                session.Io.Error("You have not received a whisper so you can't (/r)eply.");
                return;
            }
            var toUserId = session.Items[SessionItem.LastWhisperFromUserId];
            if (toUserId == null || !(toUserId is int))
            {
                session.Io.Error("You have not received a whisper so you can't (/r)eply.");
                return;
            }

            Send(session, (int)toUserId, string.Join(" ", args));
        }

        private static void Send(BbsSession session, int toUserId, string message)
        { 
            if (toUserId < 1)
            {
                session.Io.Error("User not found.");
                return;
            }
            var toUserName = session.Usernames[toUserId];

            if (true != DI.Get<ISessionsList>().Sessions?.Any(s => toUserId == s.User?.Id))
            {
                session.Io.Error($"{toUserName} is not online at this time.");
                if ('Y' == session.Io.Ask("Send this message as an E-Mail instead"))
                {
                    Mail.SendMail(session, toUserId, $"Whisper from {session.User.Name}", message);
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                    {
                        session.Io.OutputLine($"Mail sent to {toUserName}.");
                    }
                }
                return;
            }

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.DarkGray))
            {
                session.Io.OutputLine($"You whisper to {toUserName} : {message}");
                session.Messager.Publish(session, new UserMessage(session.Id, toUserId, $"{session.User.Name} whispers to you : {message}")
                {
                    TextColor = ConsoleColor.DarkGray,
                    AdditionalAction = _s => _s.Items[SessionItem.LastWhisperFromUserId] = session.User.Id
                });
            }
        }
    }
}
