using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using miniBBS.Services.Persistence;
using System;
using System.Linq;

namespace miniBBS.Services.GlobalCommands
{
    public static class AddToChatLog
    {
        /// <summary>
        /// Creates a new chat message and adds to the channel.  Returns null if the chat could not be added.
        /// </summary>
        public static Chat Execute(BbsSession session, string line, PostChatFlags postFlags = PostChatFlags.None)
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

            var chat = new Chat
            {
                DateUtc = DateTime.UtcNow,
                ChannelId = session.Channel.Id,
                FromUserId = session.User.Id,
                Message = line,
                ResponseToId = postFlags.HasFlag(PostChatFlags.IsNewTopic) ? null : session.LastReadMessageNumberWhenStartedTyping ?? session.LastReadMessageNumber
            };

            int lastRead =
                session.LastMsgPointer ??
                //session.LastReadMessageNumberWhenStartedTyping ??
                //session.LastReadMessageNumber ??
                session.MsgPointer;

            bool isAtEndOfMessages = true != session.Chats?.Any() || lastRead == session.Chats.Keys.Max();

            var chatRepo = GlobalDependencyResolver.Default.GetRepository<Chat>();
            chat = chatRepo.Insert(chat);
            session.Chats[chat.Id] = chat;
            var chatCache = GlobalDependencyResolver.Default.Get<IChatCache>().GetChannelChats(chat.ChannelId);
            if (session.Chats != chatCache && !chatCache.ContainsKey(chat.Id))
                chatCache[chat.Id] = chat;

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
            {
                session.Io.OutputLine($"Message {session.Chats.ItemNumber(chat.Id)} Posted to {session.Channel.Name}.");
                session.LastReadMessageNumber = chat.Id;
            }

            session.MarkRead(chat.Id, GlobalDependencyResolver.Default);

            if (isAtEndOfMessages)
            {
                SetMessagePointer.Execute(session, chat.Id);
                session.LastMsgPointer = session.MsgPointer;
            }
            session.Messager.Publish(session, new ChannelPostMessage(chat, session.Id));
            return chat;
        }
    }
}
