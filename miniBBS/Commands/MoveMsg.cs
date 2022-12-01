using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions_Collection;
using miniBBS.Extensions_Model;
using miniBBS.Extensions_String;
using miniBBS.Extensions_UserIo;
using miniBBS.Services.GlobalCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class MoveMsg
    {
        /// <summary>
        /// Moves one or more messages from the current channel to the specified channel
        /// </summary>
        public static void Execute(BbsSession session, params string[] args)
        {
            if (args == null || args.Length < 2)
            {
                session.Io.Error("Usgae: /ch movemsg (target channel) (msg number(s))");
                session.Io.Error("ex: /ch movemsg General 13");
                session.Io.Error("ex: /ch movemsg 3 13-30");
                session.Io.Error("ex: /ch movemsg DevNotes 13-30 32 33-45");
                return;
            }

            var channelRepo = DI.GetRepository<Channel>();
            var flagRepo = session.UcFlagRepo;
            Channel targetChannel = GetChannel.Execute(session, args[0]);
            if (targetChannel == null)
            {
                session.Io.Error("Invalid target channel name or number");
                return;
            }
            var targetFlags = flagRepo.Get(new Dictionary<string, object>
            {
                {nameof(UserChannelFlag.ChannelId), targetChannel.Id},
                {nameof(UserChannelFlag.UserId), session.User.Id}
            });
            
            // either admin, global moderator, or moderator of both source & target channels
            bool canProceed =
                session.User.Access.HasFlag(AccessFlag.Administrator) ||
                session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                (true == targetFlags?.Any(f => f.Flags.HasFlag(UCFlag.Moderator)) &&
                 true == session.UcFlag?.Flags.HasFlag(UCFlag.Moderator) );

            if (!canProceed)
            {
                session.Io.Error("Access denied");
                return;
            }

            var chatNums = ParseChatNumbers(args.Skip(1)).ToList();
            var chatsToMove = GetChatsToMove(session.Chats, chatNums).ToList();

            if (Confirm(session, chatsToMove, targetChannel.Name))
            {
                var chatRepo = DI.GetRepository<Chat>();
                foreach (var chat in chatsToMove)
                    MoveMessage(session, chat, targetChannel, chatRepo);
            }
        }

        private static void MoveMessage(BbsSession session, Chat chatToMove, Channel targetChannel, IRepository<Chat> chatRepo)
        {
            chatToMove.ChannelId = targetChannel.Id;
            chatRepo.Update(chatToMove);
            session.Chats.Remove(chatToMove.Id);

            var chatString = chatToMove.GetWriteString(session);
            var msg1 = $"{session.User.Name} moved the following message from this channel to {targetChannel.Name}{Environment.NewLine}{chatString}";
            var msg2 = $"{session.User.Name} moved the following message from another channel to here{Environment.NewLine}{chatString}";

            session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, msg1)
            {
                OnReceive = (_remoteSession) =>
                {
                    if (_remoteSession.Chats.ContainsKey(chatToMove.Id))
                        _remoteSession.Chats.Remove(chatToMove.Id);
                }
            });

            session.Messager.Publish(session, new ChannelMessage(session.Id, targetChannel.Id, msg2)
            {
                OnReceive = (_remoteSession) =>
                {
                    if (!_remoteSession.Chats.ContainsKey(chatToMove.Id))
                        _remoteSession.Chats[chatToMove.Id] = chatToMove;
                }
            });
        }

        private static bool Confirm(BbsSession session, List<Chat> chatsToMove, string targetChannelName)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Move the following chats to channel '{targetChannelName}'?");
            foreach (var chat in chatsToMove)
            {
                string username = session.Usernames.ContainsKey(chat.FromUserId) ? session.Usernames[chat.FromUserId] : "Unknown";
                builder.AppendLine($"{session.Chats.ItemNumber(chat.Id)} : {username} : {chat.Message}".MaxLength(session.Cols - 3));
            }
            session.Io.OutputLine(builder.ToString());
            return 'Y' == session.Io.Ask("Move these messages?");
        }

        private static IEnumerable<Chat> GetChatsToMove(SortedList<int, Chat> chats, List<int> chatNums)
        {
            foreach (var num in chatNums)
            {
                var id = chats.ItemKey(num);
                if (id.HasValue && chats.ContainsKey(id.Value))
                    yield return chats[id.Value];
            }
        }

        private static IEnumerable<int> ParseChatNumbers(IEnumerable<string> args)
        {
            foreach (var arg in args)
            {
                var range = ParseRange.Execute(arg, int.MaxValue);
                for (int i = range.Item1; i <= range.Item2; i++)
                    yield return i;
            }
        }
    }
}
