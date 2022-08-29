using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Services.GlobalCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class DeleteChannel
    {
        public static void Execute(BbsSession session)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
            {
                // can't delete channels without a date created (channels made prior to this feature)
                if (!session.Channel.DateCreatedUtc.HasValue)
                {
                    session.Io.OutputLine("This channel cannot be deleted.");
                    return;
                }

                // can delete if admin
                bool canDelete = session.User.Access.HasFlag(AccessFlag.Administrator);

                // or can delete if it is less that 60 minutes old
                canDelete |=
                    session.UcFlag.Flags.HasFlag(UCFlag.Moderator) &&
                    (DateTime.UtcNow - session.Channel.DateCreatedUtc.Value).TotalMinutes < Constants.MaxMinutesToDeleteChannel;

                var chatRepo = DI.GetRepository<Chat>();
                var chats = chatRepo.Get(c => c.ChannelId, session.Channel.Id).ToList();

                // or can delete if contains no messages or only messages from this user (who is also a moderator)
                canDelete |=
                    session.UcFlag.Flags.HasFlag(UCFlag.Moderator) &&
                    chats.Count(c => c.FromUserId != session.User.Id) < 1;

                if (!canDelete)
                {
                    session.Io.OutputLine("Access denied to delete this channel.");
                    return;
                }

                session.Io.Output("Are you sure you want to delete this channel?  This cannot be undone!  If you are certain type 'DELETE': ");
                string d = session.Io.InputLine();
                session.Io.OutputLine();
                if (!"DELETE".Equals(d, StringComparison.CurrentCultureIgnoreCase))
                {
                    session.Io.OutputLine("Delete aborted!");
                    return;
                }

                foreach (var chat in chats)
                    chatRepo.Delete(chat);

                var flagsRepo = DI.GetRepository<UserChannelFlag>();
                var flags = flagsRepo.Get(f => f.ChannelId, session.Channel.Id);

                foreach (var flag in flags)
                    flagsRepo.Delete(flag);

                DI.GetRepository<Channel>().Delete(session.Channel);

                session.Io.OutputLine($"Channel deleted, switching to {Constants.DefaultChannelName}.");

                SwitchOrMakeChannel.Execute(session, Constants.DefaultChannelName, allowMakeNewChannel: false);
            }
        }
    }
}
