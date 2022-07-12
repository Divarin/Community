using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Commands
{
    public static class SwitchOrMakeChannel
    {
        private static readonly string[] _invalidChannelNames = new[]
        {
            "del"
        };

        public static bool Execute(BbsSession session, string channelName)
        {
            if (channelName == null || channelName.Any(c => char.IsWhiteSpace(c)) || channelName.Length > Constants.MaxChannelNameLength || _invalidChannelNames.Contains(channelName, StringComparer.CurrentCultureIgnoreCase))
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    session.Io.OutputLine($"Invalid channel name, must not include any whitespace characters and cannot be longer than {Constants.MaxChannelNameLength} characters.");
                }
                return false;
            }

            var logger = DI.Get<ILogger>();
            var channelRepo = DI.GetRepository<Channel>();
            var chatRepo = DI.GetRepository<Chat>();

            int chanNum = -1;
            int channelId = -1;
            if (int.TryParse(channelName, out chanNum))
            {
                var chans = channelRepo
                    .Get()
                    .Where(c => c.CanJoin(session))
                    .OrderBy(c => c.Id)
                    .ToArray();
                if (chanNum >= 1 && chanNum <= chans.Length)
                    channelId = chans[chanNum - 1].Id;
            }

            Channel channel;
            if (channelId > 0)
                channel = channelRepo.Get(channelId);
            else
                channel = channelRepo.Get(c => c.Name, channelName).FirstOrDefault();

            if (channelId < 1 && channel == null)
                channel = MakeChannel(session, channelName, channelRepo);
            if (channel == null)
                return false;
            
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

            var messager = DI.Get<IMessager>();
            if (session.Channel != null) // will be null during logon while here to join default channel
                messager.Publish(new ChannelMessage(session.Id, session.Channel.Id, $"{session.User.Name} has left {session.Channel.Name}"));

            session.Channel = channel;
            session.Chats = new SortedList<int, Chat>(chatRepo.Get(c => c.ChannelId, session.Channel.Id)
                .ToDictionary(k => k.Id));
            session.UcFlag = ucFlag;
            SetMessagePointer.Execute(session, session.UcFlag.LastReadMessageNumber);
            session.ContextPointer = null;
            session.LastReadMessageNumber = null;

            //logger.Log(session, $"Changed to channel {session.Channel.Name}");

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine($"Changed to channel {channel.Name}");

                var channelUsers = DI.Get<ISessionsList>()
                    .Sessions
                    .Where(s => s.Channel?.Id == channel.Id)
                    ?.OrderBy(s => s.SessionStartUtc)
                    ?.Where(s => s.User != null)
                    ?.Select(s => $"{s.User.Name}{(s.IdleTime.TotalMinutes >= Constants.DefaultPingPongDelayMin ? " (idle)" : "")}");

                session.Io.OutputLine($"Users currently online in {channel.Name} : {string.Join(", ", channelUsers)}");
            }

            messager.Publish(new ChannelMessage(session.Id, channel.Id, $"{session.User.Name} has joined {channel.Name}"));

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

            var logger = DI.Get<ILogger>();
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
            logger.Log($"Created new channel ({channel.Id}) [{channel.Name}]");
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
