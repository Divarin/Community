using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class ListChannels
    {
        public static void Execute(BbsSession session, IRepository<Channel> channelRepo = null)
        {
            if (channelRepo == null)
                channelRepo = DI.GetRepository<Channel>();

            var userFlags = session.UcFlagRepo.Get(f => f.UserId, session.User.Id)
                .ToDictionary(k => k.ChannelId);

            //Func<Channel, bool> CanJoin = chan =>
            //{
            //    return !chan.RequiresInvite ||
            //        session.User.Access.HasFlag(AccessFlag.Administrator) ||
            //        session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
            //        (userFlags.ContainsKey(chan.Id) && (userFlags[chan.Id].Flags & (UCFlag.Invited | UCFlag.Moderator)) > 0);
            //};

            var chans = channelRepo
                .Get()
                .Where(c => c.CanJoin(session))// CanJoin(c))
                .OrderBy(c => c.Id)
                .ToArray();

            var chatRepo = DI.GetRepository<Chat>();

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                session.Io.OutputLine("# : Channel Name   (unread)");
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
                    builder.AppendLine($"{i+1} : {chan.Name} ({unread}) {(chan.RequiresInvite ? " (Invite Only)" : "")}");
                }
                session.Io.OutputLine(builder.ToString());
            }
        }
    }
}
