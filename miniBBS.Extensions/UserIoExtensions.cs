using miniBBS.Core;
using miniBBS.Interfaces;
using System;

namespace miniBBS.Extensions
{
    public static class UserIoExtensions
    {
        public static char Ask(this IUserIo io, string question)
        {
            if (io == null)
                return (char)0;

            using (io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                io.Output($"{question}: ");
                var result = io.InputKey();
                io.OutputLine();
                return char.ToUpper(result ?? (char)0);
            }
        }

        public static void Error(this IUserIo io, string message)
        {
            if (io == null)
                return;

            using (io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                io.OutputLine(message);
        }

        public static string WrapInColor(string message, ConsoleColor color)
        {
            return $"{Constants.InlineColorizer}{(int)color}{Constants.InlineColorizer}{message}{Constants.InlineColorizer}-1{Constants.InlineColorizer}";
        }

        public static void Pause(this IUserIo io)
        {
            if (io == null)
                return;

            using (io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
            {
                io.Output("[Press Any Key]");
                io.InputKey();
                io.OutputLine();
            }
        }
    }
}
