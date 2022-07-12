using miniBBS.Core;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Menus
{
    public static class Bells
    {
        private static readonly string[] _lines = new[]
        {
            "*** Bell Notifications ***",
            $"{Constants.Spaceholder}",
            "This feature will (attempt to) sound an audible tone on certain events.",
            "This requires either support for the BELL character (ASCII 7) or ANSI music, or both.",
            "/bell : tests the sound so that you can confirm your terminal will work with it.",
            "/bell on : will turn on the bell if any user connects or posts a message in the channel you're in.",
            "/bell off : turns off the bell for all events",
            "/bell (username) : turns on the bell if (username) connects or posts a message in the channel you're in.",
            $"{Constants.Spaceholder}{Constants.Spaceholder}{Constants.Spaceholder}Example: '/bell jimbob' will sound the bell if jimbob logs in or when he posts a message."
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
