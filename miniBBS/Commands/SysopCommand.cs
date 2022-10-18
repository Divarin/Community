using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using miniBBS.Persistence;
using miniBBS.Services;
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

            var di = GlobalDependencyResolver.Default;
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
                case "webcompile":
                    di.Get<IWebLogger>().UpdateWebLog(di);
                    session.Io.Error("Web log compiled.");
                    break;
                case "webstop":
                    di.Get<IWebLogger>().StopContinuousRefresh();
                    session.Io.Error("Web log continuous refresh stopped.");
                    break;
                case "webstart":
                    di.Get<IWebLogger>().StartContinuousRefresh(di);
                    session.Io.Error("Web log continuous refresh started.");
                    break;
                case "webstate":
                    session.Io.Error($"Web log continuous refresh is going?  {di.Get<IWebLogger>().ContinuousRefresh}");
                    break;
                case "maint":
                    DatabaseMaint.Maint(session);
                    break;
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
            builder.AppendLine("ip - Lists banned IPs");
            builder.AppendLine("ip ban (ip) - adds ip (or mask) to IP ban list");
            builder.AppendLine("ip unban (ip) - removes ip (or mask) from IP ban list");
            builder.AppendLine("webcompile - compile the web log");
            builder.AppendLine("webstart - starts automatic compilation of the web log every 2 hours (if any new chats)");
            builder.AppendLine("webstop - stops automatic compilation of the web log");
            builder.AppendLine("webstate - shows whether or not auto compilation is started");
            builder.AppendLine("maint - run maintenence");

            session.Io.Output(builder.ToString());
        }
    }
}
