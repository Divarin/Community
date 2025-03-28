﻿using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Extensions;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using miniBBS.Services.GlobalCommands;
using System;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class SessionInfo
    {
        public static void Execute(BbsSession session, params string[] args)
        {
            if ("times".Equals(args?.FirstOrDefault(), StringComparison.CurrentCultureIgnoreCase))
            {
                ShowSessionTimes(session);
            }
            else if (args == null || args.Length <= 1)
            {
                ShowSessionInfo(session, args?.FirstOrDefault() ?? session.User.Name);
            }
            else if (session.User.Access.HasFlag(AccessFlag.Administrator) && "flag".Equals(args[0]))
            {
                if (args.Length < 2)
                    session.Io.OutputLine($"{session.ControlFlags}");
                else if (Enum.TryParse(args[1], true, out SessionControlFlags f))
                {
                    if (session.ControlFlags.HasFlag(f))
                        session.ControlFlags &= ~f;
                    else
                        session.ControlFlags |= f;
                    session.Io.OutputLine($"{session.ControlFlags}");
                }
            }
        }

        public static void Ghosts(BbsSession session)
        {
            var userSessions = DI.Get<ISessionsList>().Sessions
                .Where(x => x.User?.Id == session.User.Id)
                .OrderBy(x => x.SessionStartUtc)
                .ToList();

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                var builder = new StringBuilder();
                builder.AppendLine("*** Your Sessions ***".Color(ConsoleColor.Yellow));
                for (var i=0; i < userSessions.Count; i++)
                {
                    var userSession = userSessions[i];
                    var current = userSession == session ? " (this session)".Color(ConsoleColor.Magenta) : string.Empty;
                    var slated = userSession.ForceLogout ? " (kick pending)".Color(ConsoleColor.Red) : string.Empty;
                    builder.AppendLine($"{i + 1} : {userSession.SessionStartUtc.AddHours(session.TimeZone):H:mm:ss} @ {userSession.CurrentLocation.FriendlyName()}{current}{slated}");
                }
                session.Io.OutputLine(builder.ToString());
                session.Io.SetForeground(ConsoleColor.White);
                session.Io.Output("Kill which session? or (A)ll (except this one), or (Q)uit:");
                var line = session.Io.InputLine();
                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Q", StringComparison.CurrentCultureIgnoreCase))
                    return;
                if (int.TryParse(line, out var killSessionNum) && killSessionNum >= 1 && killSessionNum <= userSessions.Count)
                {
                    userSessions[killSessionNum - 1].SetForcedLogout("Busting specific Ghost for this user");
                    var msg = $"{session.User.Name} is killing a ghost.";
                    DI.Get<IMessager>().Publish(session, new ChannelMessage(session.Id, session.Channel.Id, msg));
                    session.Io.Error($"It may take up to {Constants.MaxLoginTimeMin} minutes for the ghost to be killed.");
                }
                if (line.StartsWith("A", StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (var s in userSessions.Where(s => s != session))
                    {
                        s.SetForcedLogout("Busting (A)ll ghosts for this user");
                    }
                    var msg = $"{session.User.Name} is killing ghosts.";
                    DI.Get<IMessager>().Publish(session, new ChannelMessage(session.Id, session.Channel.Id, msg));
                    session.Io.Error($"It may take up to {Constants.MaxLoginTimeMin} minutes for the ghosts to be killed.");
                }
            }

        }

        private static void ShowSessionInfo(BbsSession session, string username)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("*** Sessions Info ***");
                var sessions = DI.Get<ISessionsList>().Sessions;
                if (!string.IsNullOrWhiteSpace(username))
                    sessions = sessions.Where(s => username.Equals(s.User?.Name, StringComparison.CurrentCultureIgnoreCase));

                if (!session.User.Access.HasFlag(AccessFlag.Administrator))
                    sessions = sessions.Where(s => !s.ControlFlags.HasFlag(SessionControlFlags.Invisible));

                foreach (var s in sessions)
                {
                    builder.AppendLine($"Session ID: {s.Id}");
                    builder.AppendLine($"Sesssion Start: {s.SessionStartUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm:ss}");
                    builder.AppendLine($"User: {s.User?.Name}");
                    builder.AppendLine($"Doing?: {s.CurrentLocation}");
                    builder.AppendLine($"Total Logins: {s.User?.TotalLogons}");
                    builder.AppendLine($"Channel: {s.Channel?.Name}");
                    builder.AppendLine($"User Access: {s.User?.Access}");
                    builder.AppendLine($"Control Flags: {s.ControlFlags}");
                    builder.AppendLine($"Channel Flags: {s.UcFlag?.Flags}");
                    if (session.User.Access.HasFlag(AccessFlag.Administrator))
                        builder.AppendLine($"IP Address: {s.IpAddress}");
                    builder.AppendLine($"Idle Time: {s.IdleTime:hh\\:mm\\:ss}");
                    builder.AppendLine($"Do Not Disturb?: {s.DoNotDisturb}");
                    builder.AppendLine($"Time Zone: {s.TimeZone}");
                    builder.AppendLine($"Terminal: {s.Cols} x {s.Rows}  {s.Io?.EmulationType}");
                    builder.AppendLine("------------------------------------");
                }

                session.Io.OutputLine(builder.ToString());
            }
        }
    
        private static void ShowSessionTimes(BbsSession session)
        {
            var sessions = DI.Get<ISessionsList>().Sessions
                .Where(s => s.User != null)
                .OrderBy(s => s.User.Name)
                .ToList();

            var now = DateTime.UtcNow;

            var builder = new StringBuilder();

            builder.AppendLine($"{now:yy-MM-dd HH:mm:ss} - UTC");
            var bbsUptime = now - SysopScreen.StartedAtUtc;
            builder.AppendLine($"{DateTime.Now:yy-MM-dd HH:mm:ss} - {Constants.BbsName} ({Constants.BbsLocation}) ({bbsUptime.Dhm()})");
            builder.AppendLine(" --- Active Sessions ---".Color(ConsoleColor.DarkGray));

            foreach (var s in sessions)
            {
                var offset = s.TimeZone >= 0 ? $"+{s.TimeZone}" : $"{s.TimeZone}";
                var duration = now - s.SessionStartUtc;
                builder.AppendLine($"{now.AddHours(s.TimeZone):yy-MM-dd HH:mm:ss} (UTC{offset}) - User {s.User.Name.Color(ConsoleColor.White)} ({duration.Dhm()})");
            }

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                session.Io.OutputLine(builder.ToString());
        }
    }
}
