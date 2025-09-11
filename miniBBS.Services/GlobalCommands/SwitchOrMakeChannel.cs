using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using miniBBS.Services.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Services.GlobalCommands
{
    public static class SwitchOrMakeChannel
    {
        public static void ToggleArchive(BbsSession session)
        {
            var showChatArchive =
                session.Items.ContainsKey(SessionItem.ShowChatArchive)
                && (bool)session.Items[SessionItem.ShowChatArchive];

            showChatArchive = !showChatArchive;

            session.Items[SessionItem.ShowChatArchive] = showChatArchive;
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                session.Io.OutputLine($"Archvied chat messages will now be: {(showChatArchive ? "Shown".Color(ConsoleColor.Green) : "Hidden".Color(ConsoleColor.Red))}");
                session.Io.OutputLine("Use '" + "/archive".Color(ConsoleColor.Yellow) + "' to toggle this.");
            }

            SetSessionChats(session);

            // re-show current chat message
            int? currentMessage = session.ContextPointer ?? session.LastReadMessageNumber;
            if (!currentMessage.HasValue || session.Chats?.ContainsKey(currentMessage.Value) != true)
            {
                // when going back to "archive" mode, the message pointer might be pointing to an archived message
                // if that's the case try to change it to the lowest message that is not archived.
                currentMessage = session.Chats?.Keys?.FirstOrDefault();
                if (currentMessage.HasValue)
                    session.LastReadMessageNumber = currentMessage;
            }
            if (currentMessage.HasValue && true == session.Chats?.ContainsKey(currentMessage.Value))
            {
                var currentChat = session.Chats[currentMessage.Value];
                currentChat.Write(session, ChatWriteFlags.None, GlobalDependencyResolver.Default);
            }
        }

        public static bool Execute(BbsSession session, string channelNameOrNumber, bool allowMakeNewChannel, bool fromMessageBase = false)
        {
            bool invalidChannelName =
                channelNameOrNumber == null ||
                channelNameOrNumber.Any(c => char.IsWhiteSpace(c)) ||
                channelNameOrNumber.Length > Constants.MaxChannelNameLength ||
                Constants.InvalidChannelNames.Contains(channelNameOrNumber, StringComparer.CurrentCultureIgnoreCase);

            if (invalidChannelName)
            {
                session.Io.Error($"Invalid channel name, must not include any whitespace characters and cannot be longer than {Constants.MaxChannelNameLength} characters.");
                return false;
            }

            var logger = GlobalDependencyResolver.Default.Get<ILogger>();
            var channelRepo = GlobalDependencyResolver.Default.GetRepository<Channel>();
            var chatRepo = GlobalDependencyResolver.Default.GetRepository<Chat>();

            Channel channel = GetChannel.Execute(session, channelNameOrNumber);

            if (channel == null && allowMakeNewChannel)
                channel = MakeChannel(session, channelNameOrNumber, channelRepo);

            if (channel == null)
                return false;

            if (session.Items.ContainsKey(SessionItem.CrossChannelNotificationReceivedChannels))
            {
                var list = session.Items[SessionItem.CrossChannelNotificationReceivedChannels] as List<int>;
                if (true == list?.Contains(channel.Id))
                {
                    list.Remove(channel.Id);
                }
            }

            var ucFlag = session.UcFlagRepo.Get(new Dictionary<string, object>
            {
                {nameof(UserChannelFlag.ChannelId), channel.Id},
                {nameof(UserChannelFlag.UserId), session.User.Id}
            })?.FirstOrDefault() ?? new UserChannelFlag
            {
                ChannelId = channel.Id,
                UserId = session.User.Id
            };

            bool canJoin =
                !channel.RequiresInvite ||
                session.User.Access.HasFlag(AccessFlag.Administrator) ||
                session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                ucFlag.Flags.HasFlag(UCFlag.Invited) ||
                ucFlag.Flags.HasFlag(UCFlag.Moderator);

            if (!canJoin)
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    session.Io.OutputLine("Cannot join channel, invite only and you're not invited!");
                }
                return false;
            }

            var messager = GlobalDependencyResolver.Default.Get<IMessager>();
            if (session.Channel != null) // will be null during logon while here to join default channel
                messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, $"{session.User.Name} has left {session.Channel.Name} for {channel.Name}."));

            session.Channel = channel;
            SetSessionChats(session);

            session.UcFlag = ucFlag;
            SetMessagePointer.Execute(session, session.UcFlag.LastReadMessageNumber);

            session.ContextPointer = null;
            session.LastReadMessageNumber = null;

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine($"Changed to channel {channel.Name}");

                var channelUsers = GlobalDependencyResolver.Default.Get<ISessionsList>()
                    .Sessions
                    .Where(s => s.Channel?.Id == channel.Id)
                    ?.OrderBy(s => s.SessionStartUtc)
                    ?.Where(s => s.User != null)
                    ?.GroupBy(s => s.User.Name)
                    ?.Select(s =>
                    {
                        string _username = s.Key;
                        var flags = new List<string>();
                        if (s.All(x => x.Afk)) flags.Add("AFK");
                        if (s.All(x => x.DoNotDisturb)) flags.Add("DND");
                        if (s.All(x => x.IdleTime.TotalMinutes >= Constants.DefaultPingPongDelayMin)) flags.Add("IDLE");
                        if (flags.Any())
                            _username += $" ({string.Join(",", flags)})";
                        return _username;
                    });

                session.Io.OutputLine($"Users currently online in {channel.Name} : {string.Join(", ", channelUsers)}");
            }

            if (!fromMessageBase)
            {
                messager.Publish(session, new ChannelMessage(session.Id, channel.Id, $"{session.User.Name} has joined {channel.Name}"));

                var flags =
                    ChatWriteFlags.UpdateLastMessagePointer |
                    ChatWriteFlags.UpdateLastReadMessage |
                    ChatWriteFlags.DoNotShowMessage;

                ShowNextMessage.Execute(session, flags);
            }

            return true;
        }

        public static void SetSessionChats(BbsSession session)
        {
            var chatCache = GlobalDependencyResolver.Default
                            .Get<IChatCache>()
                            .GetChannelChats(session.Channel.Id);

            var filterChatsToUnarchived = chatCache.Keys.Count > Constants.MaxUnarchivedChats;
            if (session.Items.ContainsKey(SessionItem.ShowChatArchive) &&
                (bool)session.Items[SessionItem.ShowChatArchive] == true)
            {
                filterChatsToUnarchived = false;
            }

            if (!filterChatsToUnarchived)
            {
                // user has unlocked archived messages so they have unfiltered access to the chat cache.
                session.Chats = chatCache;
            }
            else
            {
                // user has not unlocked archived messages so they will see a filtered subset of the chat cache.
                var today = DateTime.UtcNow.Date;

                // start by filtering by date (recent chats only)
                var filteredChats = chatCache
                    .Values
                    .Where(c => (today - c.DateUtc.Date).TotalDays <= Constants.ArchivedChatDays)
                    .ToList();

                if (filteredChats.Count < Constants.MaxUnarchivedChats)
                {
                    // not enough recent messages to have the desired number of chats (50)
                    // so we'll ignore the date filter and just take the most recent 50, regardless of age.
                    var skip = chatCache.Count - Constants.MaxUnarchivedChats;
                    if (skip <= 0)
                    {
                        // there's not enough messages to bother filtering
                        session.Chats = chatCache;
                        return;
                    }
                    filteredChats = chatCache.Values.Skip(skip).ToList();
                }

                session.Chats = new SortedList<int, Chat>(filteredChats
                    .ToDictionary(k => k.Id));
            }
        }

        private static Channel MakeChannel(BbsSession session, string channelName, IRepository<Channel> channelRepo)
        {
            if (!ValidateChannelName(channelName))
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    session.Io.OutputLine($"Invalid name for new channel: '{channelName}'.  Channel names must contain only letters and no spaces.");
                }
                return null;
            }

            if ('Y' != session.Io.Ask($"Create new channel, '{channelName}'"))
                return null;

            var logger = GlobalDependencyResolver.Default.Get<ILogger>();
            var channel = channelRepo.Insert(new Channel
            {
                Name = channelName,
                RequiresInvite = false,
                DateCreatedUtc = DateTime.UtcNow
            });
            session.UcFlagRepo.Insert(new UserChannelFlag
            {
                ChannelId = channel.Id,
                UserId = session.User.Id,
                LastReadMessageNumber = 0,
                Flags = UCFlag.Moderator
            });
            logger.Log(session, $"Created new channel ({channel.Id}) [{channel.Name}]");
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                session.Io.OutputLine($"Created channel {channel.Name}.  If this was a mistake use '/ch del' to delete the channel.");
            }
            return channel;
        }

        private static bool ValidateChannelName(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName) || channelName.Any(c => !char.IsLetter(c)))
                return false;
            return true;
        }
    }
}
