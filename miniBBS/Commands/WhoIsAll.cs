using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class WhoIsAll
    {
        public static void Execute(BbsSession session, params string[] args)
        {
            var sort = session.Io.Ask("1) Most recent login\r\n2) User's first login\r\n3) Most Calls\r\n4) Alphabetical\r\nSort By");

            IEnumerable<User> users = session.UserRepo.Get();
            switch (sort)
            {
                case '1': users = users.OrderByDescending(u => u.LastLogonUtc); break;
                case '2': users = users.OrderBy(u => u.DateAddedUtc); break;
                case '3': users = users.OrderByDescending(u => u.TotalLogons); break;
                case 'Q': return;
                default: users = users.OrderBy(u => u.Name); break;
            }                

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
                    s.DoNotDisturb,
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

            if (true == args?.Any() && int.TryParse(args[0], out int n))
                users = users.Where(u => u.TotalLogons >= n);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("* Community users *".Color(ConsoleColor.Magenta));
            switch (sort)
            {
                case '1': builder.AppendLine("Last Login      Username".Color(ConsoleColor.White)); break;
                case '2': builder.AppendLine("First Login     Username".Color(ConsoleColor.White)); break;
                case '3': builder.AppendLine("Num Calls  Username".Color(ConsoleColor.White)); break;
                default: builder.AppendLine("Username".Color(ConsoleColor.White)); break;
            }

            foreach (var u in users)
            {
                string l;
                switch (sort)
                {
                    case '1': l = $"{u.LastLogonUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm}".PadRight(16) + u.Name; break;
                    case '2': l = $"{u.LastLogonUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm}".PadRight(16) + u.Name; break;
                    case '3': l = u.TotalLogons.ToString().PadRight(11) + u.Name; break;
                    default: l = u.Name; break;
                }
                
                if (online.ContainsKey(u.Name))
                {
                    var usl = online[u.Name];
                    var afk = usl.FirstOrDefault(x => x.Afk);
                    if (afk != null)
                    {
                        if (!"away from keyboard".Equals(afk.AfkReason, StringComparison.CurrentCultureIgnoreCase))
                            l += $" (AFK:{afk.AfkReason})".Color(ConsoleColor.Red);
                        else
                            l += " (AFK)".Color(ConsoleColor.Red);
                    }
                    var dnd = usl.FirstOrDefault(x => x.DoNotDisturb);
                    if (dnd != null)
                        l += " (DND)".Color(ConsoleColor.Red);
                    l += $" in {string.Join(", ", usl.Select(x => x.ChannelName).Distinct())}".Color(ConsoleColor.Yellow);
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
