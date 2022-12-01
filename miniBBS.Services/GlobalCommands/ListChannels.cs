using miniBBS.Core;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions_Model;
using miniBBS.Extensions_ReadTracker;
using miniBBS.Extensions_String;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Services.GlobalCommands
{
    public static class ListChannels
    {
        public static void Execute(BbsSession session, IRepository<Channel> channelRepo = null)
        {
            if (channelRepo == null)
                channelRepo = GlobalDependencyResolver.Default.GetRepository<Channel>();

            var userFlags = session.UcFlagRepo.Get(f => f.UserId, session.User.Id)
                .ToDictionary(k => k.ChannelId);
            
            Channel[] chans = GetChannelList(session, channelRepo);

            var chatRepo = GlobalDependencyResolver.Default.GetRepository<Chat>();

            int longestChannelName = chans.Max(c => c.Name.Length) + 1;
            var readIds = session.ReadChatIds(GlobalDependencyResolver.Default);

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                session.Io.OutputLine($"#   : Channel Name {' '.Repeat(longestChannelName - "Channel Name".Length)}Unread");
            }

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
                    var unread = GetUnreadCount(session, readIds, chatRepo, chan.Id);

                    builder.Append($"{Constants.InlineColorizer}{(int)ConsoleColor.Cyan}{Constants.InlineColorizer}{i + 1,-3}");
                    builder.Append($" : {Constants.InlineColorizer}-1{Constants.InlineColorizer}");
                    builder.Append($"{chan.Name} {' '.Repeat(longestChannelName - chan.Name.Length)}");
                    if (unread > 0)
                        builder.Append($"{Constants.InlineColorizer}{(int)ConsoleColor.Magenta}{Constants.InlineColorizer}");
                    else
                        builder.Append($"{Constants.InlineColorizer}{(int)ConsoleColor.Gray}{Constants.InlineColorizer}");
                    builder.AppendLine($"{unread}{Constants.InlineColorizer}-1{Constants.InlineColorizer}");
                }

                session.Io.Output(builder.ToString());
            }
        }

        public static Channel[] GetChannelList(BbsSession session)
        {
            var repo = GlobalDependencyResolver.Default.GetRepository<Channel>();
            return GetChannelList(session, repo);
        }

        private static Channel[] GetChannelList(BbsSession session, IRepository<Channel> channelRepo)
        {
            return channelRepo
                .Get()
                .Where(c => c.CanJoin(session))
                .OrderBy(c => c.Id)
                .ToArray();
        }

        private static int GetUnreadCount(BbsSession session, List<int> readIds, IRepository<Chat> chatRepo, int channelId)
        {
            var di = GlobalDependencyResolver.Default;            
            var count = chatRepo
                .Get(c => c.ChannelId, channelId)
                ?.Count(c => !readIds.Contains(c.Id)) ?? 0;
            return count;
        }
    }
}
