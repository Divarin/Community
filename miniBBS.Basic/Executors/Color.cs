using miniBBS.Basic.Interfaces;
using miniBBS.Basic.Models;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Basic.Executors
{
    public static class Color
    {
        public static void Execute(BbsSession session, string line, Variables variables)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;
            var parts = line.Split(',');

            if (parts.Length >= 1 && !string.IsNullOrWhiteSpace(parts[0]))
                SetForeground(session, ParseColor(session, parts[0], variables));
            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                SetBackground(session, ParseColor(session, parts[1], variables));
        }

        private static ConsoleColor ParseColor(BbsSession session, string color, Variables variables)
        {
            color = color?.Replace("\"", "")?.Trim();
            if (!string.IsNullOrWhiteSpace(color))
            {
                color = Evaluate.Execute(session, color, variables);
                ConsoleColor result;
                if (Enum.TryParse(color, true, out result))
                    return result;

                int i;
                if (int.TryParse(color, out i) && i>=0 && i<=15)
                    return (ConsoleColor)i;
            }

            return ConsoleColor.Black;
        }

        private static void SetBackground(BbsSession session, ConsoleColor color)
        {
            session.Io.SetBackground(color);
        }

        private static void SetForeground(BbsSession session, ConsoleColor color)
        {
            session.Io.SetForeground(color);
        }
    }
}
