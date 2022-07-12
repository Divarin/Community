using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using System;
using System.Linq;

namespace miniBBS.Commands
{
    public static class ListModerators
    {
        public static void Execute(BbsSession session)
        {
            var mods = session.UcFlagRepo.Get(x => x.ChannelId, session.Channel.Id)
                ?.Where(x => x.Flags.HasFlag(UCFlag.Moderator));

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine($"{session.Channel.Name} moderators:");
                if (true == mods?.Any())
                {
                    string text = string.Join(Environment.NewLine, mods.Select(m => session.Usernames.ContainsKey(m.UserId) ? session.Usernames[m.UserId] : $"??? (User ID: {m.UserId}"));
                    session.Io.OutputLine(text);
                }
            }
        }
    }
}
