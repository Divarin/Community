using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using System;
using System.Linq;

namespace miniBBS.Commands
{
    public static class ListInvitations
    {
        public static void Execute(BbsSession session)
        {
            var invites = session.UcFlagRepo.Get(x => x.ChannelId, session.Channel.Id)
                ?.Where(x => x.Flags.HasFlag(UCFlag.Invited));

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine($"{session.Channel.Name} invitations:");
                if (true == invites?.Any())
                {
                    string text = string.Join(Environment.NewLine, invites.Select(i => session.Usernames.ContainsKey(i.UserId) ? session.Usernames[i.UserId] : $"??? (User ID: {i.UserId}"));
                    session.Io.OutputLine(text);
                }
            }
        }
    }
}
