using miniBBS.Core;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace miniBBS.Services.GlobalCommands
{
    public static class ListChannels
    {
        private const int _totalUnreadToNotifiyAboutIndexes = 100;

        public static void Execute(BbsSession session, IRepository<Channel> channelRepo = null)
        {
            var originalColor = session.Io.GetForeground();

            try
            {
                if (channelRepo == null)
                    channelRepo = GlobalDependencyResolver.Default.GetRepository<Channel>();

                var userFlags = session.UcFlagRepo.Get(f => f.UserId, session.User.Id)
                    .ToDictionary(k => k.ChannelId);

                Channel[] chans = GetChannelList(session, channelRepo);

                var chatRepo = GlobalDependencyResolver.Default.GetRepository<Chat>();

                int longestChannelName = chans.Max(c => c.Name.Length) + 1;
                var readIds = session.ReadChatIds(GlobalDependencyResolver.Default);

                session.Io.SetForeground(ConsoleColor.Magenta);
                var header = $"#   : Channel Name {' '.Repeat(longestChannelName - "Channel Name".Length)}Unread";
                session.Io.OutputLine(header);
                session.Io.SetForeground(ConsoleColor.DarkGray);
                session.Io.OutputLine('-'.Repeat(header.Length));

                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
                {
                    StringBuilder builder = new StringBuilder();

                    for (int i = 0; i < chans.Length; i++)
                    {
                        var chan = chans[i];
                        int lastRead;
                        if (session.UcFlag.ChannelId == chan.Id)
                            lastRead = session.UcFlag.LastReadMessageNumber;
                        else if (userFlags.ContainsKey(chan.Id))
                            lastRead = userFlags[chan.Id].LastReadMessageNumber;
                        else
                            lastRead = -1;
                        //var unread = chatRepo.GetCountWhereProp1EqualsAndProp2IsGreaterThan<int, int>(x => x.ChannelId, chan.Id, x => x.Id, lastRead);
                        var unread = GetChannelCount(readIds, chatRepo, chan.Id).SubsetCount;

                        builder.Append($"{Constants.InlineColorizer}{(int)ConsoleColor.Cyan}{Constants.InlineColorizer}{i + 1,-3}");
                        builder.Append($" : {Constants.InlineColorizer}-1{Constants.InlineColorizer}");
                        builder.Append($"{chan.Name} {' '.Repeat(longestChannelName - chan.Name.Length)}");
                        if (unread > 0)
                            builder.Append($"{Constants.InlineColorizer}{(int)ConsoleColor.Magenta}{Constants.InlineColorizer}");
                        else
                            builder.Append($"{Constants.InlineColorizer}{(int)ConsoleColor.Gray}{Constants.InlineColorizer}");
                        builder.AppendLine($"{unread}{Constants.InlineColorizer}-1{Constants.InlineColorizer}");
                    }
                    
                    builder.AppendLine("Change channels with [, ], or /ch #".Color(ConsoleColor.Blue));
                    session.Io.Output(builder.ToString());
                }
            } 
            finally
            {
                session.Io.SetForeground(originalColor);
            }
        }

        public static Channel[] GetChannelList(BbsSession session)
        {
            var repo = GlobalDependencyResolver.Default.GetRepository<Channel>();
            return GetChannelList(session, repo);
        }

        public static Count Count(BbsSession session)
        {
            var di = GlobalDependencyResolver.Default;
            var readIds = session.ReadChatIds(di);
            var chatRepo = di.GetRepository<Chat>();
            var chans = GetChannelList(session);

            var total = new Count();

            foreach (var chan in chans)
            {
                var channelCount = GetChannelCount(readIds, chatRepo, chan.Id);
                total.TotalCount += channelCount.TotalCount;
                total.SubsetCount += channelCount.SubsetCount;
            }

            return total;            
        }

        private static Channel[] GetChannelList(BbsSession session, IRepository<Channel> channelRepo)
        {
            return channelRepo
                .Get()
                .Where(c => c.CanJoin(session))
                .OrderBy(c => c.Id)
                .ToArray();
        }

        private static Count GetChannelCount(List<int> readIds, IRepository<Chat> chatRepo, int channelId)
        {
            var di = GlobalDependencyResolver.Default;
            var chats = chatRepo.Get(c => c.ChannelId, channelId);

            return new Count
            {
                TotalCount = chats?.Count() ?? 0,
                SubsetCount = chats?.Count(c => !readIds.Contains(c.Id)) ?? 0
            };
        }
    }
}
