using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using miniBBS.Services;
using System;
using System.Linq;

namespace miniBBS.Commands
{
    public static class RenameChannel
    {
        public static void Execute(BbsSession session)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
            {
                // can rename if admin or moderator
                bool canRename =
                    session.User.Access.HasFlag(AccessFlag.Administrator) ||
                    session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                    session.UcFlag.Flags.HasFlag(UCFlag.Moderator);

                if (!canRename)
                {
                    session.Io.OutputLine("Access denied to rename this channel.");
                    return;
                }

                var oldname = session.Channel.Name;
                session.Io.Output($"Enter new channel name, enter=Keep Current ({oldname}): ");
                var newname = session.Io.InputLine()?.Trim();
                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(newname))
                {
                    session.Io.OutputLine("rename aborted!");
                    return;
                }

                bool invalidChannelName =
                    newname.Any(c => char.IsWhiteSpace(c)) ||
                    newname.Length > Constants.MaxChannelNameLength ||
                    Constants.InvalidChannelNames.Contains(newname, StringComparer.CurrentCultureIgnoreCase);

                if (invalidChannelName)
                {
                    session.Io.Error($"Invalid channel name, must not include any whitespace characters and cannot be longer than {Constants.MaxChannelNameLength} characters.");
                    return;
                }

                var repo = DI.GetRepository<Channel>();
                var chanRec = repo.Get(session.Channel.Id);
                chanRec.Name = newname;
                repo.Update(chanRec);

                session.Io.OutputLine($"Channel renamed, switching to {Constants.DefaultChannelName}.");

                var logger = GlobalDependencyResolver.Default.Get<ILogger>();
                var msg = $"Renamed channel '{oldname}' to '{newname}'";
                logger.Log(session, msg);
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
                {
                    session.Io.OutputLine(msg);
                }
                session.Channel.Name = newname;
            }
        }
    }
}
