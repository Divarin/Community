using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Menus
{
    public static class Bells
    {
        private static Func<string, ConsoleColor, string> _clr = (str, clr) => UserIoExtensions.WrapInColor(str, clr);

        //  {_clr("", ConsoleColor.Green)}

        //    12345678901234567890123456789012345678901234567890123456789012345678901234567890
        //    1234567890123456789012345678901234567890
        private static readonly string[] _lines = new[]
        {
            $"*** {_clr("Bell Notifications", ConsoleColor.Yellow)} ***",
            $"{Constants.Spaceholder}",
            $"This feature will (attempt to) sound an audible tone on certain events.",
            $"This requires either support for the BELL character (ASCII 7) or ANSI music, or both.",
            $"{_clr("/bell", ConsoleColor.Green)} : tests the sound so that you can confirm your terminal will work with it.",
            $"{_clr("/bell on", ConsoleColor.Green)} : will turn on the bell if any user connects or posts a message in the channel you're in.",
            $"{_clr("/bell off", ConsoleColor.Green)} : turns off the bell for all events",
            $"{_clr("/bell (username)", ConsoleColor.Green)} : turns on the bell if (username) connects or posts a message in the channel you're in.",
            $"{Constants.Spaceholder}{Constants.Spaceholder}{Constants.Spaceholder}Example: '/bell jimbob' will sound the bell if jimbob logs in or when he posts a message."
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
