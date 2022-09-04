using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using System;
using System.Linq;

namespace miniBBS.Services.GlobalCommands
{
    public static class AddToChatLog
    {
        /// <summary>
        /// Creates a new chat message and adds to the channel.  Returns null if the chat could not be added.
        /// </summary>
        public static Chat Execute(BbsSession session, IRepository<Chat> chatRepo, string line, bool isNewTopic = false)
        {
            var canPost =
                true == session.User?.Access.HasFlag(AccessFlag.Administrator) ||
                true == session.User?.Access.HasFlag(AccessFlag.GlobalModerator) ||
                true == session.UcFlag.Flags.HasFlag(UCFlag.Moderator) ||
                true != session.Channel?.RequiresVoice ||
                session.UcFlag.Flags.HasFlag(UCFlag.HasVoice);

            if (!canPost)
            {
                session.Io.Error("Sorry you're not allowed to talk in this channel at this time.  You may require 'voice', to request voice type '/voice'.");
                return null;
            }

            Chat chat = new Chat
            {
                DateUtc = DateTime.UtcNow,
                ChannelId = session.Channel.Id,
                FromUserId = session.User.Id,
                Message = line,
                ResponseToId = isNewTopic ? null : session.LastReadMessageNumberWhenStartedTyping ?? session.LastReadMessageNumber
            };

            int lastRead =
                session.LastReadMessageNumberWhenStartedTyping ??
                session.LastReadMessageNumber ??
                session.MsgPointer;

            bool isAtEndOfMessages = true != session.Chats?.Any() || lastRead == session.Chats.Keys.Max();
            chat = chatRepo.Insert(chat);
            session.Chats[chat.Id] = chat;
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
            {
                session.Io.OutputLine($"Message {session.Chats.ItemNumber(chat.Id)} Posted to {session.Channel.Name}.");
                session.LastReadMessageNumber = chat.Id;
            }
            if (isAtEndOfMessages)
                SetMessagePointer.Execute(session, chat.Id);
            session.Messager.Publish(new ChannelPostMessage(chat, session.Id));
            return chat;
        }
    }
}
