using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using System;
using System.Linq;

namespace miniBBS.Commands
{
    public static class ToggleChannelModerator
    {
        public static void Execute(BbsSession session, string username)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
            {
                if (!session.UcFlag.Flags.HasFlag(UCFlag.Moderator) && !session.User.Access.HasFlag(AccessFlag.Administrator))
                {
                    session.Io.OutputLine("Access denied.");
                    return;
                }

                var user = session.UserRepo.Get(u => u.Name, username)?.FirstOrDefault();
                if (user == null)
                {
                    session.Io.OutputLine("No such user.");
                    return;
                }
                var userFlags = session.UcFlagRepo.Get(f => f.UserId, user.Id)?.FirstOrDefault();
                if (userFlags == null)
                    userFlags = new UserChannelFlag
                    {
                        UserId = user.Id,
                        ChannelId = session.Channel.Id
                    };

                if (userFlags.Flags.HasFlag(UCFlag.Moderator))
                    userFlags.Flags &= ~UCFlag.Moderator;
                else
                    userFlags.Flags |= UCFlag.Moderator;

                session.UcFlagRepo.InsertOrUpdate(userFlags);

                string message = $"{session.User.Name} has {(userFlags.Flags.HasFlag(UCFlag.Moderator) ? "" : "un-")}made {user.Name} to moderator on channel {session.Channel.Name}.";

                var messager = DI.Get<IMessager>();
                messager.Publish(new ChannelMessage(session.Id, session.Channel.Id, message));
                messager.Publish(new UserMessage(session.Id, user.Id, message));
                DI.Get<INotificationHandler>().SendNotification(user.Id, message);
                DI.Get<ILogger>().Log(message);
                session.Io.OutputLine(message);
            }
        }
    }
}
