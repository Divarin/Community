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
            var sort = session.Io.Ask(string.Format("1) Most recent login{0}2) User's first login{0}3) Most Calls{0}4) Alphabetical{0}A) Aggregate{0}Sort By", session.Io.NewLine));

            IEnumerable<User> users = session.UserRepo.Get();
            switch (sort)
            {
                case '1': users = users.OrderByDescending(u => u.LastLogonUtc); break;
                case '2': users = users.OrderBy(u => u.DateAddedUtc); break;
                case '3': users = users.OrderByDescending(u => u.TotalLogons); break;
                case 'A': 
                    users = Aggregate(session, users);
                    if (users == null)
                        return;
                    break;
                case 'Q': return;
                default: users = users.OrderBy(u => u.Name); break;
            }

            var online = DI.Get<ISessionsList>()
                .Sessions
                ?.Where(s => users.Any(u => u.Id == s.User?.Id))
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

            session.Io.Output($"There are {users.Count()} total users");
            if (online?.Count > 0)
                session.Io.Output($", {online.Count} {(online.Count == 1 ? "is" : "are")} online.");
            session.Io.OutputLine();
            session.Io.OutputLine("Slacker : One who has failed to call in the past 30 days.");
            if ('Y' == session.Io.Ask("Filter out slackers"))
                users = users.Where(u => u.LastLogonUtc >= DateTime.UtcNow.AddMonths(-1));

            if (true == args?.Any() && int.TryParse(args[0], out int n))
                users = users.Where(u => u.TotalLogons >= n);

            var builder = new StringBuilder();
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
                    case '2': l = $"{u.DateAddedUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm}".PadRight(16) + u.Name; break;
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

        private static IEnumerable<User> Aggregate(BbsSession session, IEnumerable<User> users)
        {
            session.Io.OutputLine("Aggregate by:");
            session.Io.OutputLine("1) Terminal Emulation");
            session.Io.OutputLine("2) Time Zone");
            var ag = session.Io.Ask("Aggregate by");

            Dictionary<string, List<User>> dict;
            string grouping;
            switch (ag)
            {
                case '1':
                    grouping = "Emualtion ";
                    dict = users.GroupBy(x => $"{x.Emulation}").ToDictionary(k => k.Key, v => v.ToList());
                    break;
                case '2':
                    grouping = "Time Zone ";
                    dict = users.GroupBy(x => $"{x.Timezone}").ToDictionary(k => k.Key, v => v.ToList());
                    break;
                default:
                    return null;
            }

            session.Io.OutputLine($"#    {grouping} Count".Color(ConsoleColor.White));
            string[] keys;
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                keys = dict
                    .OrderByDescending(x => x.Value.Count())
                    .Select(x => x.Key)
                    .ToArray();

                for (int i=0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    session.Io.OutputLine($"{i + 1,-4} {key,-10} {dict[key].Count()}");
                }
            }

            var n = session.Io.AskWithNumber("List users in group #, or (Q)uit");
            if (!string.IsNullOrWhiteSpace(n) && int.TryParse(n, out var groupNum) && groupNum >= 1 && groupNum <= keys.Length+1)
            {
                var groupUsers = dict[keys[groupNum-1]];
                return groupUsers;
            }

            return null;
        }
    }
}
