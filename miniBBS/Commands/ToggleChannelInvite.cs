using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using System.Linq;

namespace miniBBS.Commands
{
    public static class ToggleChannelInvite
    {
        public static void Execute(BbsSession session, string username)
        {
            using (session.Io.WithColorspace(System.ConsoleColor.Black, System.ConsoleColor.Red))
            {
                if (!session.Channel.RequiresInvite)
                {
                    session.Io.OutputLine("Channel is not invite only.");
                    return;
                }

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

                if (userFlags.Flags.HasFlag(UCFlag.Invited))
                    userFlags.Flags &= ~UCFlag.Invited;
                else
                    userFlags.Flags |= UCFlag.Invited;

                session.UcFlagRepo.InsertOrUpdate(userFlags);

                string message = $"{session.User.Name} has {(userFlags.Flags.HasFlag(UCFlag.Invited) ? "" : "un")}invited {user.Name} to channel {session.Channel.Name}.";

                var messager = DI.Get<IMessager>();
                messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, message));
                messager.Publish(session, new UserMessage(session.Id, user.Id, message));
                DI.Get<INotificationHandler>().SendNotification(user.Id, message);                
                DI.Get<ILogger>().Log(session, message);
                session.Io.OutputLine(message);
            }
        }
    }
}
