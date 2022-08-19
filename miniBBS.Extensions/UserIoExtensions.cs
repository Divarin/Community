using miniBBS.Interfaces;
using System;

namespace miniBBS.Extensions
{
    public static class UserIoExtensions
    {
        public static char Ask(this IUserIo io, string question)
        {
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
            using (io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                io.OutputLine(message);
        }
    }
}
