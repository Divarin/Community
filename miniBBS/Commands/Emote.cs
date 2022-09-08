using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using System;
using System.Linq;

namespace miniBBS.Commands
{
    public static class Emote
    {
        public static void Execute(BbsSession session, string[] args)
        {
            string emote = $"{session.User.Name} {GetEmoteString(args[0])}";
            int? targetUserId = null;
            if (args.Length >= 2)
            {
                string targetUserName = args[1].Trim();
                var targetUser = session.Usernames.FirstOrDefault(u => u.Value.Equals(targetUserName, StringComparison.CurrentCultureIgnoreCase));
                
                if (targetUser.Value != null)
                    targetUserId = targetUser.Key;
                else
                {
                    targetUser = session.Usernames.FirstOrDefault(u => u.Value.StartsWith(targetUserName, StringComparison.CurrentCultureIgnoreCase));
                    if (targetUser.Value != null)
                        targetUserId = targetUser.Key;
                }

            if (!ValidateTargetUser(session.Channel.Id, targetUserId))
                {
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                    {
                        session.Io.OutputLine($"{targetUserName} is not online or in this channel at this time.");
                    }
                    return;
                }
            }

            if (targetUserId.HasValue)
                emote += $" {session.Usernames[targetUserId.Value]}";
            else
                emote += " the channel";

            var emoteMessage = new EmoteMessage(session.Id, session.User.Id, session.Channel.Id, targetUserId, emote);

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Green))
            {
                session.Io.OutputLine(emote);
            }

            session.Messager.Publish(session, emoteMessage);
        }

        private static bool ValidateTargetUser(int channelId, int? targetUserId)
        {
            if (!targetUserId.HasValue)
                return false;

            var sessionsList = DI.Get<ISessionsList>();
            var targetSession = sessionsList.Sessions.FirstOrDefault(s => s.Channel?.Id == channelId && s.User?.Id == targetUserId.Value);
            if (targetSession == null)
                return false;

            return true;
        }

        private static string GetEmoteString(string command)
        {
            switch (command.ToLower())
            {
                case "/wave": return "waves to";
                case "/poke": return "pokes";
                case "/smile": return "smiles at";
                case "/frown": return "frowns at";
                case "/wink": return "winks at";
                case "/nod": return "nods to";
                case "/fairwell":
                case "/farewell":
                case "/goodbye":
                case "/bye":
                    return "bids goodbye to";
                default: return command.Substring(1);
            }
        }
    }
}
