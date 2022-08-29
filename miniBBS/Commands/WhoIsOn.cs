using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Commands
{
    public static class WhoIsOn
    {
        public static void Execute(BbsSession session, ISessionsList sessionsList)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine($"* Who is on Community right now *");

                var online = DI.Get<ISessionsList>()
                    .Sessions
                    ?.Where(s => s.User != null && s.Channel != null)
                    ?.Select(s => new
                    {
                        Username = s.User.Name,
                        Afk = s.Afk,
                        AfkReason = s.AfkReason,
                        IdleTime = s.IdleTime,
                        ChannelName = s.Channel.Name
                    })
                    .GroupBy(s => s.Username)
                    .ToDictionary(k => k.Key, v => v.ToList());

                List<string> list = new List<string>();
                foreach (var s in online)
                {
                    string listItem = $"{s.Key} ";
                    var usl = online[s.Key];
                    var afk = usl.FirstOrDefault(x => x.Afk);
                    if (afk != null)
                    {
                        if (!"away from keyboard".Equals(afk.AfkReason, StringComparison.CurrentCultureIgnoreCase))
                            listItem += $"(AFK:{afk.AfkReason})";
                        else
                            listItem += "(AFK)";
                    }
                    listItem += $" in {string.Join(", ", usl.Select(x => x.ChannelName).Distinct())}";
                    var idleTime = usl.Min(x => x.IdleTime.TotalMinutes);
                    if (idleTime >= 5)
                    {
                        int h = (int)Math.Floor(idleTime / 60);
                        int m = (int)Math.Round(idleTime % 60);
                        if (h > 0)
                            listItem += $" - {h}h {m}m idle";
                        else
                            listItem += $" - {m} min. idle";
                    }
                    list.Add(listItem);
                }

                list.Add($"{Environment.NewLine}{UserIoExtensions.WrapInColor("Use '/users' to see full user list.", ConsoleColor.Magenta)}");
                string result = string.Join(Environment.NewLine, list);
                session.Io.OutputLine(result);
            }

            //var option = session.Io.Ask("Do you want to see a list of all users? (Y)es, (N)o, (D)on't ask again");

        }
    }
}
