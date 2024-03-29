﻿using miniBBS.Core;
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
            session.Chats = GlobalDependencyResolver.Default.Get<IChatCache>().GetChannelChats(session.Channel.Id);
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
                //using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
                //{
                //    session.Io.OutputLine($"This is where you left off in {session.Channel.Name}:");
                //}

                var flags = 
                    ChatWriteFlags.UpdateLastMessagePointer |
                    ChatWriteFlags.UpdateLastReadMessage |
                    ChatWriteFlags.DoNotShowMessage;

                ShowNextMessage.Execute(session, flags);
            }

            return true;
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
