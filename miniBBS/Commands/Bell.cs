using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Commands
{
    public static class Bell
    {
        public static void Execute(BbsSession session, string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                session.Io.OutputLine("Type '/bell off' to disable all bell sounds.");
                session.Io.OutputLine("Type '/bell on' to enable bell sounds when someone connects or posts a message.");
                session.Io.OutputLine("Type '/bell jimbob' to enable bell sounds when jimbob connects or posts a message.");
                session.Io.OutputLine("(Obviously, replace 'jimbob' with the user you actually want)");
                Test(session);
            }
            else if ("off".Equals(args, StringComparison.CurrentCultureIgnoreCase))
            {
                session.BellAlerts = null;
                session.Io.OutputLine("All bell sounds are now disabled.");
            }
            else
            {
                session.BellAlerts = args;
                if ("on".Equals(args, StringComparison.CurrentCultureIgnoreCase))
                    session.Io.OutputLine("Bell sounds will be sent whenever anyone connects or posts a message in your active channel.");
                else
                    session.Io.OutputLine($"Bell sounds will be send whwnever {args} connects or posts a message in your active channe.");
                session.Io.OutputLine("Type '/bell off' to disable.");
            }
        }

        public static void Test(BbsSession session)
        {
            session.Io.OutputLine("Testing bell ...");
            Notify(session);
        }

        public static void Notify(BbsSession session)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Black))
            {
                session.Io.Output((char)7);
                if (session.Io.EmulationType == TerminalEmulation.Ansi)
                    session.Io.Output("\u001b[MF T120 O2C8D8E8F8G8 " + (char)14);
            }
        }
    }
}
