using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using System;
using System.Data;

namespace miniBBS.Commands
{
    public static class Calculate
    {
        private static readonly DataTable _computer = new DataTable();

        public static void Execute(BbsSession session, params string[] args)
        {
            var expression = string.Join(" ", args);
            if (string.IsNullOrWhiteSpace(expression))
                return;
            
            var result = _computer.Compute(expression, string.Empty)?.ToString();
            var message = $"{session.User.Name} calculates {expression} = {result}";

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine(message);
            }
            session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, message));
        }
    }
}
