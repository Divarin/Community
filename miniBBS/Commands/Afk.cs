﻿using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using System;

namespace miniBBS.Commands
{
    public static class Afk
    {
        public static void Execute(BbsSession session, string reason)
        {
            session.Afk = !session.Afk;

            if (string.IsNullOrWhiteSpace(reason))
                reason = "away from keyboard";

            if (reason.Length > Constants.MaxAfkReasonLength)
                reason = reason.Substring(0, Constants.MaxAfkReasonLength);
            
            session.AfkReason = session.Afk ? reason : null;

            string afk = $"{(session.Afk ? "" : "no longer ")}AFK ({reason}).";
            session.Messager.Publish(new ChannelMessage(session.Id, session.Channel.Id, $"{session.User.Name} is {afk}"));
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine($"You are {afk}");
            }
        }
    }
}
