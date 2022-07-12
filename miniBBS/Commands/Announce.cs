using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class Announce
    {
        public static void Execute(BbsSession session, string announcement)
        {
            if (!session.User.Access.HasFlag(AccessFlag.Administrator))
                return;

            var chans = DI.Get<ISessionsList>().Sessions
                .Where(s => s.Channel != null)
                .Select(s => s.Channel.Id)
                .Distinct()
                .ToArray();

            string msg = $"Global announcement from {session.User.Name}: {announcement}";
            foreach (var chan in chans)
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                {
                    session.Io.OutputLine(msg);
                }

                session.Messager.Publish(new ChannelMessage(session.Id, chan, msg));
            }
        }
    }
}
