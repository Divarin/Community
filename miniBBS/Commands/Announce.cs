using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using System;

namespace miniBBS.Commands
{
    public static class Announce
    {
        public static void Execute(BbsSession session, string announcement)
        {
            if (!session.User.Access.HasFlag(AccessFlag.Administrator))
                return;

            string msg = $"Global announcement from {session.User.Name}: {announcement}";

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine(msg);
            }
            session.Messager.Publish(new GlobalMessage(session.Id, msg, disturb: true));

        }
    }
}
