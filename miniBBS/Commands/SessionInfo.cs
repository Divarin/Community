using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using System;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class SessionInfo
    {
        public static void Execute(BbsSession session, params string[] args)
        {
            if (args==null || args.Length <= 1)
                ShowSessionInfo(session, args?.FirstOrDefault());

            if (!session.User.Access.HasFlag(AccessFlag.Administrator))
                return;

            if ("flag".Equals(args[0]))
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
                    builder.AppendLine("---------------------------------------");
                }

                session.Io.OutputLine(builder.ToString());
            }
        }
    }
}
