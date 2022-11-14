using miniBBS.Basic.Interfaces;
using miniBBS.Basic.Models;
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
            line = Evaluate.Execute(line, variables);

            if (newlineAfter)
                session.Io.OutputLine(line);
            else
                session.Io.Output(line);
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
                .Replace("\"", "");
            
            line = Evaluate.Execute(line, variables);

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
