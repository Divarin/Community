using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using miniBBS.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class SysopCommand
    {
        public static void Execute(BbsSession session, ref List<string> ipBans, params string[] args)
        {
            if (!session.User.Access.HasFlag(AccessFlag.Administrator) || true != args?.Any())
                return;

            switch (args[0].ToLower())
            {
                case "?":
                case "help":
                    ShowUsage(session);
                    break;
                case "chatid": 
                    session.Io.OutputLine(session.Chats.ItemKey(int.Parse(args[1])).ToString());
                    break;
                case "chatnum":
                    session.Io.OutputLine(session.Chats.ItemNumber(int.Parse(args[1])).ToString());
                    break;
                case "user":
                    ManageUser(session, args.Skip(1).ToArray());
                    break;
                case "ip":
                    ManageIps(session, ref ipBans, args.Skip(1).ToArray());
                    break;
                case "maint":
                    DatabaseMaint.Maint(session);
                    break;
                case "newthread":
                case "chain":
                    ChainMessages(session, startNewThread: "newthread".Equals(args[0], StringComparison.CurrentCultureIgnoreCase), args.Skip(1).ToArray());
                    break;
                case "menucache":
                    DI.Get<IMenuFileLoader>().ClearCache();
                    session.Io.Error("Menu cache cleared.");
                    break;
            }
        }

        private static void ChainMessages(BbsSession session, bool startNewThread, params string[] args)
        {
            if (args == null || args.Length < 2 || !int.TryParse(args[0], out int start) || !int.TryParse(args[1], out int end) || end <= start)
            {
                session.Io.Error("Usage: '/sysop newthread startMsg# endMsg#' or '/sysop chain startMsg# endMsg#'.");
                return;
            }

            var originalFlags = session.ControlFlags;
            session.ControlFlags |= SessionControlFlags.DoNotSendNotifications;
            try
            { 
                if (startNewThread)
                    EditMessage.ReassignReNumber(session, start.ToString(), "none");

                for (int i=start+1; i <= end; i++)
                    EditMessage.ReassignReNumber(session, i.ToString(), (i-1).ToString());
            }
            finally
            {
                session.ControlFlags = originalFlags;
            }
        }

        private static void ManageIps(BbsSession session, ref List<string> ipBans, params string[] args)
        {
            var repo = DI.GetRepository<Core.Models.Data.IpBan>();

            if (true != args?.Any())
            {
                // list banned IPs
                string list = string.Join(Environment.NewLine, repo.Get().Select(x => x.IpMask));
                session.Io.OutputLine(list);
            }
            else if (args.Length == 2)
            {
                if ("ban".Equals(args[0], StringComparison.CurrentCultureIgnoreCase))
                    IpBan.Execute(session, ref ipBans, args[1]);
                else if ("unban".Equals(args[0], StringComparison.CurrentCultureIgnoreCase))
                    IpBan.Execute(session, ref ipBans, "r", args[1]);
            }
        }

        private static void ManageUser(BbsSession session, params string[] args)
        {
            var user = session.UserRepo.Get(u => u.Name, args[0])
                ?.FirstOrDefault();
            if (user == null)
            {
                session.Io.Error($"User '{args[0]}' not found.");
                return;
            }

            if (args.Length < 2)
            {
                // show user access
                UserInfo.Execute(session, args[0]);
                return;
            }

            switch (args[1].ToLower())
            {
                case "ban":
                    user.Access &= ~AccessFlag.MayLogon;
                    session.UserRepo.Update(user);
                    session.Io.Error($"{user.Name} is now banned!");
                    break;
                case "unban":
                    user.Access |= AccessFlag.MayLogon;
                    session.UserRepo.Update(user);
                    session.Io.Error($"{user.Name} is no longer banned!");
                    break;
                case "del":
                    if ('Y' == session.Io.Ask($"Delete '{user.Name}'? This cannot be undone!"))
                    {
                        session.UserRepo.Delete(user);
                        session.Io.Error($"User '{user.Name}' deleted.");
                    }
                    break;
                case "ips":
                    var userLogs = DI.GetRepository<LogEntry>().Get(l => l.UserId, user.Id)
                        ?.Where(l => l.Message.Contains("has logged in"))
                        ?.GroupBy(l => l.IpAddress)
                        ?.ToDictionary(k => k.Key, v => new
                        {
                            Count = v.Count(),
                            Earliest = v.Min(x => x.TimestampUtc),
                            Latest = v.Max(x => x.TimestampUtc)
                        });

                    string results = string.Join(Environment.NewLine, userLogs.Select(l => $"{l.Key} : {l.Value.Count} ({l.Value.Earliest.AddHours(session.TimeZone)} - {l.Value.Latest.AddHours(session.TimeZone)})"));

                    session.Io.OutputLine(results);

                    break;
            }
        }

        private static void ShowUsage(BbsSession session)
        {
            var builder = new StringBuilder();
            
            builder.AppendLine("chatid n - shows the ID of chat # n");
            builder.AppendLine("chatnum n - shows the chat # of ID n");
            builder.AppendLine("user (username) - info about the user (same as '/ui (username)')");
            builder.AppendLine("user (username) ban - removes user's MayLogin access flag");
            builder.AppendLine("user (username) unban - adds user's MayLogin access flag");
            builder.AppendLine("user (username) ips - find all IPs the user has logged in with");
            builder.AppendLine("user (username) del - delete user, with confirmation");
            builder.AppendLine("ip - Lists banned IPs");
            builder.AppendLine("ip ban (ip) - adds ip (or mask) to IP ban list");
            builder.AppendLine("ip unban (ip) - removes ip (or mask) from IP ban list");
            builder.AppendLine("maint - run maintenence");
            builder.AppendLine("newthread (n1) (n2) - chains contiguous messages together using re: numbers starting with message # n1 and ending with n2.  n1 is marked as 'new thread'.");
            builder.AppendLine("chain (n1) (n2) - chains contiguous messages together using re: numbers starting with message # n1 and ending with n2.  n1's re: number is left untouched.");
            builder.AppendLine("menucache - cleared cached menu file data, use if a menu file was updated.");

            session.Io.Output(builder.ToString());
        }
    }
}
