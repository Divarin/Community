using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Services.GlobalCommands
{
    /// <summary>
    /// Finds and returns a channel by the <paramref name="channelNameOrNumber"/>.  Will return null 
    /// if the channel doesn't exist or if the user doesn't have access to it.
    /// </summary>
    public static class GetChannel
    {
        /// <summary>
        /// Finds and returns a channel by the <paramref name="channelNameOrNumber"/>.  Will return null 
        /// if the channel doesn't exist or if the user doesn't have access to it.
        /// </summary>
        public static Channel Execute(BbsSession session, string channelNameOrNumber)
        {
            var channelRepo = GlobalDependencyResolver.GetRepository<Channel>();

            int chanNum = -1;
            int channelId = -1;
            if (int.TryParse(channelNameOrNumber, out chanNum))
            {
                var chans = GetChannels(session);
                if (chanNum >= 1 && chanNum <= chans.Count)
                    channelId = chans[chanNum - 1].Id;
            }

            Channel channel;
            if (channelId > 0)
                channel = channelRepo.Get(channelId);
            else
                channel = channelRepo.Get(c => c.Name, channelNameOrNumber).FirstOrDefault();

            return channel;
        }

        /// <summary>
        /// Gets the list of channels that the user can join.
        /// </summary>
        public static List<Channel> GetChannels(BbsSession session)
        {
            var channelRepo = GlobalDependencyResolver.GetRepository<Channel>();
            return GetChannels(session, channelRepo);
        }

        public static List<Channel> GetChannels(BbsSession session, IRepository<Channel> channelRepo)
        { 
            var chans = channelRepo
                .Get()
                .Where(c => c.CanJoin(session))
                .OrderBy(c => c.Id)
                .ToList();

            return chans;
        }
    }
}
