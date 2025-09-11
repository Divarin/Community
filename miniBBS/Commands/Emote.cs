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
            if (!FindTargetUser(session, args, out int? targetUserId))
                return;

            string emote = $"{GetEmoteString(session, args[0])}";

            if (targetUserId.HasValue)
                emote += $" {session.Usernames[targetUserId.Value]}";
            else if ("/me".Equals(args[0], StringComparison.CurrentCultureIgnoreCase))
                emote += " " + string.Join(" ", args.Skip(1));
            else
                emote += " everybody";

            var emoteMessage = new EmoteMessage(session.Id, session.User.Id, session.Channel.Id, targetUserId, emote);

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                session.Io.OutputLine(emote);
            }

            session.Messager.Publish(session, emoteMessage);
        }

        /// <summary>
        /// Tries to find the <paramref name="targetUserId"/>.  Returns false if something went wrong and processing the command should abort 
        /// otherwise returns true, even if the target User ID is not populated.
        /// </summary>
        private static bool FindTargetUser(BbsSession session, string[] args, out int? targetUserId)
        {
            targetUserId = null;

            bool isOnlineMsg =
                "/online".Equals(args[0], StringComparison.CurrentCultureIgnoreCase) ||
                "/onl".Equals(args[0], StringComparison.CurrentCultureIgnoreCase) ||
                "/on".Equals(args[0], StringComparison.CurrentCultureIgnoreCase) ||
                "/me".Equals(args[0], StringComparison.CurrentCultureIgnoreCase);

            if (!isOnlineMsg && args.Length >= 2)
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
                    return false;
                }
            }

            return true;
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

        private static string GetEmoteString(BbsSession session, string command)
        {
            switch (command.ToLower())
            {
                case "/wave": return $"* {session.User.Name} waves to";
                case "/poke": return $"* {session.User.Name} pokes";
                case "/smile": return $"* {session.User.Name} smiles at";
                case "/frown": return $"* {session.User.Name} frowns at";
                case "/wink": return $"* {session.User.Name} winks at";
                case "/nod": return $"* {session.User.Name} nods to";
                case "/fairwell":
                case "/farewell":
                case "/goodbye":
                case "/bye":
                    return $"* {session.User.Name} bids goodbye to";
                case "/me": return $"* {session.User.Name}";
                default: return $"* {session.User.Name} {command.Substring(1)}";
            }
        }
    }
}
