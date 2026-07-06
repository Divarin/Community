using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;

namespace miniBBS.Commands
{
    public static class Doing
    {
        public static void Execute(BbsSession session, string doing)
        {
            string message;

            if (string.IsNullOrWhiteSpace(doing) && session.Items.ContainsKey(SessionItem.Doing))
            {
                session.Items.Remove(SessionItem.Doing);
                message = $"{session.User.Name} is no longer doing anything.";
            }
            else
            {
                session.Items[SessionItem.Doing] = doing;
                message = $"{session.User.Name} is now {doing}.";
            }

            var channelMessage = new ChannelMessage(
                session.Id,
                session.Channel.Id,
                message,
                predicate: x => !x.DoNotDisturb);

            session.Messager.Publish(session, channelMessage);

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                if (!string.IsNullOrWhiteSpace(doing))
                    session.Io.OutputLine($"You are now doing '{doing}'.");
                else
                    session.Io.OutputLine("You are no longer doing anything.");
            }
        }

        public static void TryLoadFromWasDoing(BbsSession session, IRepository<Metadata> metaRepo)
        {
            var meta = metaRepo.Get(new Dictionary<string, object>
            {
                {nameof(Metadata.UserId), session.User.Id},
                {nameof(Metadata.Type), MetadataType.WasDoing}
            })?.PruneAllButMostRecent(metaRepo);

            if (meta == null || string.IsNullOrWhiteSpace(meta.Data as string))
                return;

            if ('Y' == session.Io.Ask($"Are you still doing '{meta.Data}'", color: ConsoleColor.Yellow))
            {
                session.Items[SessionItem.Doing] = meta.Data;
            }
        }
    }
}
