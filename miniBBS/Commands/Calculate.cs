using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace miniBBS.Commands
{
    public static class Calculate
    {
        private static readonly DataTable _computer = new DataTable();

        public static void Execute(BbsSession session, params string[] args)
        {
            if (args == null || args.Length < 1)
                return;

            args = ReplaceHexValuesWithDecimal(args);

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

            string hexResult = null;
            if (int.TryParse(result, out var hr))
                hexResult = " (0x" + hr.ToString("X") + ")";

            var message = $"{session.User.Name} calculates {expression} = {result}{hexResult}";

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine(message);
            }
            session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, message));
        }

        private static string[] ReplaceHexValuesWithDecimal(string[] args)
        {
            var results = new List<string>(args);
            for (int i=0; i < results.Count; i++)
            {
                var value = results[i];
                if (value.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
                {
                    var decValue = HexToDec(value);
                    if (decValue.HasValue)
                        results[i] = $"{decValue}";
                }
            }
            return results.ToArray();
        }

        private static int? HexToDec(string value)
        {
            int? result = null;
            if (value.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    result = int.Parse(value.Substring(2), NumberStyles.HexNumber);
                }
                catch
                {
                    // do nothing
                }
            }
            return result;
        }

    }
}
