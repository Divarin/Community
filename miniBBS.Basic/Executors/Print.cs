﻿using miniBBS.Basic.Models;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using System;

namespace miniBBS.Basic.Executors
{
    public static class Print
    {
        
        public static void Execute(BbsSession session, string line, Variables variables)
        {
            if (line == null)
                line = string.Empty;

            line = line.Trim();
            bool newlineAfter = true;
            if (line.EndsWith(";"))
            {
                newlineAfter = false;
                line = line.Substring(0, line.Length - 1);
            }
            line = Evaluate.Execute(session, line, variables);

            if (newlineAfter)
                line += session.Io.NewLine;

            if (session.Io.EmulationType != Core.Enums.TerminalEmulation.Atascii)
                line = session.Io.TransformText(line);

            var bytes = session.Io.GetBytes(line);
            session.Io.OutputRaw(bytes);
        }

        public static void BroadcastToChannel(BbsSession session, string line, Variables variables)
        {
            if (line == null)
                line = string.Empty;

            line = line.Trim();
            bool newlineAfter = true;
            if (line.EndsWith(";"))
            {
                newlineAfter = false;
                line = line.Substring(0, line.Length - 1);
            }

            var botname = variables["SCRIPTNAME$"]
                .ToUpper()
                .Replace(".MBS", "BOT")
                .Replace(".BOT", "BOT")
                .Replace("\"", "");
            
            line = Evaluate.Execute(session, line, variables);

            line = $"<{botname}> {line}";

            if (newlineAfter)
                line += Environment.NewLine;

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                session.Io.Output(line);

            if (variables["DEBUGGING"] != "1")
                session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, line));
        }
    }
}
