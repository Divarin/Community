using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
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
                var header = $"{Constants.Inverser}#   : Channel Name {' '.Repeat(longestChannelName - "Channel Name".Length)}Unread{Constants.Inverser}";

                var twoColumns = session.Cols >= 80 && header.Length < 38;

                var col1Length = header.Length;
                if (twoColumns)
                    header = $"{header}  {header}";

                session.Io.OutputLine(header, OutputHandlingFlag.NoWordWrap);
                session.Io.SetForeground(ConsoleColor.DarkGray);
                session.Io.OutputLine('-'.Repeat(header.Length - 2));

                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
                {
                    var col1 = new List<string>();
                    var col2 = new List<string>();

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

                        var unread = GetChannelCount(readIds, chatRepo, chan.Id).SubsetCount;

                        var printableLine = $"{i + 1,-3} : {chan.Name} {' '.Repeat(longestChannelName - chan.Name.Length)}{unread}";
                        string line = $"{Constants.InlineColorizer}{(int)ConsoleColor.Cyan}{Constants.InlineColorizer}{Constants.Inverser}{i + 1,-3}{Constants.Inverser}";
                        line += $" : {Constants.InlineColorizer}-1{Constants.InlineColorizer}";
                        line += $"{chan.Name} {' '.Repeat(longestChannelName - chan.Name.Length)}";
                        if (unread > 0)
                            line += $"{Constants.InlineColorizer}{(int)ConsoleColor.Magenta}{Constants.InlineColorizer}";
                        else
                            line += $"{Constants.InlineColorizer}{(int)ConsoleColor.Gray}{Constants.InlineColorizer}";
                        line += $"{unread}{Constants.InlineColorizer}-1{Constants.InlineColorizer}";

                        if (!twoColumns || i < chans.Length / 2)
                        {
                            col1.Add(line);
                        }
                        else
                        {
                            var pad = col1Length - printableLine.Length;
                            if (pad > 0)
                                line = $"{' '.Repeat(pad)}{line}";
                            col2.Add(line);
                        }
                    }

                    var builder = new StringBuilder();
                    for (var i=0; i < col1.Count || i < col2.Count; i++)
                    {
                        string line;
                        if (i < col1.Count)
                        {
                            line = col1[i];
                            if (i < col2.Count)
                                line += col2[i];
                        }
                        else
                            line = col2[i];
                        
                        builder.AppendLine(line);
                    }

                    session.Io.Output(builder.ToString());
                    session.Io.OutputLine(
                        $"Change channels with {Constants.Inverser}[{Constants.Inverser}, {Constants.Inverser}]{Constants.Inverser}, or {Constants.Inverser}/ch #{Constants.Inverser}"
                        .Color(ConsoleColor.Blue), OutputHandlingFlag.NoWordWrap);
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

        private static Count GetChannelCount(SortedSet<int> readIds, IRepository<Chat> chatRepo, int channelId)
        {
            var di = GlobalDependencyResolver.Default;
            var chats = chatRepo.Get(c => c.ChannelId, channelId);

            var total = chats?.Count() ?? 0;
            var subset = chats?.Count(c => !readIds.Contains(c.Id)) ?? 0;

            return new Count
            {
                TotalCount = total,
                SubsetCount = subset
            };
        }
    }
}
