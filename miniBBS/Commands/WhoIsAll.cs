using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class WhoIsAll
    {
        public static void Execute(BbsSession session)
        {
            IEnumerable<User> users = session.UserRepo.Get().OrderByDescending(u => u.LastLogonUtc);

            var online = DI.Get<ISessionsList>()
                .Sessions
                ?.Where(s => 
                    s.User != null && 
                    s.Channel != null && 
                    (session.User.Access.HasFlag(AccessFlag.Administrator) ||
                    (!s.ControlFlags.HasFlag(SessionControlFlags.Invisible))))
                ?.Select(s => new
                {
                    Username = s.User.Name,
                    s.Afk,
                    s.AfkReason,
                    ChannelName = s.Channel.Name
                })
                .GroupBy(s => s.Username)
                .ToDictionary(k => k.Key, v => v.ToList());

            session.Io.OutputLine("Slack-er (/ˈslakər/) : One who has failed to call in the past 30 days.");
            session.Io.Output("Filter out slackers?: ");
            var key = session.Io.InputKey();
            session.Io.OutputLine();
            if (key == 'Y' || key == 'y')
                users = users.Where(u => u.LastLogonUtc >= DateTime.UtcNow.AddMonths(-1));

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("* Community users *");
            builder.AppendLine("Last Login      Total Logins   Username");
            foreach (var u in users)
            {
                string l = $"{u.LastLogonUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm}   {u.TotalLogons,4}, {u.Name}";
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
