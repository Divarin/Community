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
                SetForeground(session, ParseColor(parts[0], variables));
            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                SetBackground(session, ParseColor(parts[1], variables));
        }

        private static ConsoleColor ParseColor(string color, Variables variables)
        {
            color = color?.Replace("\"", "")?.Trim();
            if (!string.IsNullOrWhiteSpace(color))
            {
                color = Evaluate.Execute(color, variables);
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
