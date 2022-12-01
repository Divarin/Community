﻿using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions_Model;
using System;
using System.Linq;

namespace miniBBS.Commands
{
    public static class ContinuousRead
    {
        public static void Execute(BbsSession session)
        {
            var chats = DI.GetRepository<Chat>().Get(c => c.ChannelId, session.Channel.Id)
                .Where(c => c.Id >= session.MsgPointer)
                .OrderBy(c => c.Id);

            var lines = chats.Select(c => c.GetWriteString(session));
            string all = string.Join(Environment.NewLine, lines);

            session.Io.OutputLine(all);
        }
    }
}
