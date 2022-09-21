using miniBBS.Core.Enums;
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
        public static void Execute(BbsSession session)
        {
            Execute(session, DI.Get<ISessionsList>());
        }

        public static void Execute(BbsSession session, ISessionsList sessionsList)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine($"* Who is on Community right now *");

                var sessionGroups = sessionsList
                    .Sessions
                    ?.Where(s => s.User != null && s.Channel != null)
                    ?.Select(s =>
                    {
                        string username = s.User.Name;
                        if (s.ControlFlags.HasFlag(SessionControlFlags.Invisible))
                        {
                            if (session.User.Access.HasFlag(AccessFlag.Administrator))
                                username += " (Invis)";
                            else
                                return null;
                        }

                        return new
                        {
                            Username = username,
                            s.Afk,
                            s.AfkReason,
                            s.IdleTime,
                            ChannelName = s.Channel.Name
                        };
                    })
                    .Where(s => s != null)
                    .GroupBy(s => s.Username)
                    .ToDictionary(k => k.Key, v => v.ToList());

                List<string> list = new List<string>();
                foreach (var userSessions in sessionGroups)
                {
                    foreach (var s in userSessions.Value)
                    {
                        string listItem = $"{userSessions.Key} ";
                        
                        if (s.Afk)
                        {
                            if (!"away from keyboard".Equals(s.AfkReason, StringComparison.CurrentCultureIgnoreCase))
                                listItem += $"(AFK:{s.AfkReason})";
                            else
                                listItem += "(AFK)";
                        }

                        listItem += $" in {s.ChannelName}";
                        var idleTime = s.IdleTime.TotalMinutes;
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
                }
                list.Add($"{Environment.NewLine}{UserIoExtensions.WrapInColor("Use '/users' to see full user list.", ConsoleColor.Magenta)}");
                string result = string.Join(Environment.NewLine, list);
                session.Io.OutputLine(result);
            }
        }
    }
}
