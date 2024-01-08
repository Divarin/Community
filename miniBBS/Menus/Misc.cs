using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Menus
{
    public static class MiscMenu
    {
        private static readonly Func<string, ConsoleColor, string> _clr = (str, clr) => UserIoExtensions.WrapInColor(str, clr);

        //    12345678901234567890123456789012345678901234567890123456789012345678901234567890
        //    1234567890123456789012345678901234567890
        private static readonly string[] _lines = new[]
        {
            $"*** {_clr("Community Misc Menu", ConsoleColor.Yellow)} ***",
            $"{_clr("/main", ConsoleColor.Green)} : Shows the BBS's Main Menu",
            $"{_clr("/newuser", ConsoleColor.Green)} : Read new user docs",
            $"{_clr("/doors", ConsoleColor.Green)} : Finds all user-made BASIC programs and quick-launch them.",
            $"{_clr("/term", ConsoleColor.Green)} : Config Terminal",
            $"{_clr("/dnd", ConsoleColor.Green)} : Toggle Do Not Disturb",
            $"{_clr("/afk", ConsoleColor.Green)} : Toggle Away from Keyboard",
            $"{_clr("/password", ConsoleColor.Green)} : Change your password",
            $"--- {_clr("Blurbs (one-liners at login)", ConsoleColor.Yellow)} ---",
            $"{_clr("/blurb", ConsoleColor.Green)} : Shows a random blurb",
            $"{_clr("/blurb {blurb}", ConsoleColor.Green)} : Adds a new blurb",
            $"{_clr("/blurbs", ConsoleColor.Green)} : Lists all blurbs",
            $"{_clr("/blurbadmin del #", ConsoleColor.Green)} : Delete a blurb (if yours)",
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
