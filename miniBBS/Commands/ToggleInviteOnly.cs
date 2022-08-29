using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;

namespace miniBBS.Commands
{
    public static class ToggleInviteOnly
    {
        public static void Execute(BbsSession session, bool inviteOnly)
        {
            if (session.Channel.RequiresInvite == inviteOnly || !session.UcFlag.Flags.HasFlag(UCFlag.Moderator))
                return;

            var channelRepo = DI.GetRepository<Channel>();
            session.Channel.RequiresInvite = inviteOnly;
            channelRepo.Update(session.Channel);
            string message = $"{session.User.Name} has made channel {session.Channel.Name} {(inviteOnly ? "" : "not ")}invite only.";

            DI.Get<ILogger>().Log(session, message);
            session.Messager.Publish(new ChannelMessage(session.Id, session.Channel.Id, message));            
            session.Io.OutputLine(message);
        }
    }
}
