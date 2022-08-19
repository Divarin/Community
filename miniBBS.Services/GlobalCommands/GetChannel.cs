using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
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
                channel = channelRepo.Get(c => c.Name, channelNameOrNumber).FirstOrDefault();

            return channel;
        }
    }
}
