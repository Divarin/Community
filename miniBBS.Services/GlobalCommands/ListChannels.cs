using miniBBS.Core;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Linq;
using System.Text;

namespace miniBBS.Services.GlobalCommands
{
    public static class ListChannels
    {
        public static void Execute(BbsSession session, IRepository<Channel> channelRepo = null)
        {
            if (channelRepo == null)
                channelRepo = GlobalDependencyResolver.GetRepository<Channel>();

            var userFlags = session.UcFlagRepo.Get(f => f.UserId, session.User.Id)
                .ToDictionary(k => k.ChannelId);

            var chans = channelRepo
                .Get()
                .Where(c => c.CanJoin(session))
                .OrderBy(c => c.Id)
                .ToArray();

            var chatRepo = GlobalDependencyResolver.GetRepository<Chat>();

            int longestChannelName = chans.Max(c => c.Name.Length)+1;

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                session.Io.OutputLine($"#   : Channel Name {' '.Repeat(longestChannelName - "Channel Name".Length)}Unread");
            }

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
            {
                StringBuilder builder = new StringBuilder();
                
                for (int i=0; i < chans.Length; i++)
                {
                    var chan = chans[i];
                    int lastRead;
                    if (session.UcFlag.ChannelId == chan.Id)
                        lastRead = session.UcFlag.LastReadMessageNumber;
                    else if (userFlags.ContainsKey(chan.Id))
                        lastRead = userFlags[chan.Id].LastReadMessageNumber;
                    else
                        lastRead = -1;
                    var unread = chatRepo.GetCountWhereProp1EqualsAndProp2IsGreaterThan<int, int>(x => x.ChannelId, chan.Id, x => x.Id, lastRead);
                    builder.Append($"{Constants.InlineColorizer}{(int)ConsoleColor.Cyan}{Constants.InlineColorizer}{i + 1,-3}");
                    builder.Append($" : {Constants.InlineColorizer}-1{Constants.InlineColorizer}");
                    builder.Append($"{chan.Name} {' '.Repeat(longestChannelName-chan.Name.Length)}");
                    if (unread > 0)
                        builder.Append($"{Constants.InlineColorizer}{(int)ConsoleColor.Magenta}{Constants.InlineColorizer}");
                    else
                        builder.Append($"{Constants.InlineColorizer}{(int)ConsoleColor.Gray}{Constants.InlineColorizer}");
                    builder.AppendLine($"{unread}{Constants.InlineColorizer}-1{Constants.InlineColorizer}");
                }

                session.Io.Output(builder.ToString());
            }
        }
    }
}
