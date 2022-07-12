using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using System;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class WhoIsAll
    {
        public static void Execute(BbsSession session)
        {
            var users = session.UserRepo.Get().OrderByDescending(u => u.LastLogonUtc);

            var online = DI.Get<ISessionsList>()
                .Sessions
                ?.Where(s => s.User != null && s.Channel != null)
                ?.Select(s => new
                {
                    Username = s.User.Name,
                    Afk = s.Afk,
                    AfkReason = s.AfkReason,
                    ChannelName = s.Channel.Name
                })
                .GroupBy(s => s.Username)
                .ToDictionary(k => k.Key, v => v.ToList());

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("* Community users (most recent logon) *");
            foreach (var u in users)
            {
                string l = $"{u.LastLogonUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm} {u.Name} ";
                if (online.ContainsKey(u.Name))
                {
                    var usl = online[u.Name];
                    var afk = usl.FirstOrDefault(x => x.Afk);
                    if (afk != null)
                    {
                        if (!"away from keyboard".Equals(afk.AfkReason, StringComparison.CurrentCultureIgnoreCase))
                            l += $"(AFK:{afk.AfkReason})";
                        else
                            l += "(AFK)";
                    }
                    l += $" in {string.Join(", ", usl.Select(x => x.ChannelName).Distinct())}";
                }

                builder.AppendLine(l);
            }

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine(builder.ToString());
            }
        }
    }
}
