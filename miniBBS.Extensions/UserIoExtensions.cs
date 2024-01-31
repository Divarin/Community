using miniBBS.Core;
using miniBBS.Core.Enums;
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
                io.Output($"{Constants.Inverser}{question}:{Constants.Inverser} ");
                var result = io.InputKey();
                io.OutputLine();
                return char.ToUpper(result ?? (char)0);
            }
        }

        /// <summary>
        /// Returns keypress or if a number then allows for multiple keypresses to accomodate multi-digit numbers.
        /// </summary>
        public static string AskWithNumber(this IUserIo io, string question)
        {
            if (io == null)
                return string.Empty;

            using (io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                io.Output($"{Constants.Inverser}{question}:{Constants.Inverser} ");
                var result = io.InputLine(InputHandlingFlag.ReturnFirstCharacterOnlyUnlessNumeric);
                io.OutputLine();
                return result;
            }
        }

        public static void Error(this IUserIo io, string message)
        {
            if (io == null)
                return;

            using (io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                io.OutputLine($"{Constants.Inverser}{message}{Constants.Inverser}");
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
                io.Output($"{Constants.Inverser}[Press Any Key]{Constants.Inverser}");
                io.InputKey();
                io.OutputLine();
            }
        }
    }
}
