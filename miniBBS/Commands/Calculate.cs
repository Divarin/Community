using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
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

            string result;
            try
            {
                result = _computer.Compute(expression, string.Empty)?.ToString();
            } catch (Exception ex)
            {
                session.Io.Error($"Error evaluating expression: {ex.Message}");
                return;
            }

            var message = $"{session.User.Name} calculates {expression} = {result}";

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine(message);
            }
            session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, message));
        }
    }
}
