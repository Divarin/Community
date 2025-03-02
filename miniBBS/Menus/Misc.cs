using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Menus
{
    public static class MiscMenu
    {
        private static readonly Func<string, ConsoleColor, string> _clr = (str, clr) => UserIoExtensions.WrapInColor(str, clr);

        private static readonly string[] _lines = new[]
        {
            $"{Constants.Inverser}*** {_clr("Community Misc Menu", ConsoleColor.Yellow)} ***{Constants.Inverser}",
            $"{_clr($"{Constants.Inverser}/main{Constants.Inverser}", ConsoleColor.Green)} : Shows the BBS's Main Menu",
            $"{_clr($"{Constants.Inverser}/newuser{Constants.Inverser}", ConsoleColor.Green)} : Read new user docs",
            $"{_clr($"{Constants.Inverser}/doors{Constants.Inverser}", ConsoleColor.Green)} : Finds all user-made BASIC programs and quick-launch them.",
            $"{_clr($"{Constants.Inverser}/term{Constants.Inverser}", ConsoleColor.Green)} : Config Terminal",
            $"{_clr($"{Constants.Inverser}/dnd{Constants.Inverser}", ConsoleColor.Green)} : Toggle Do Not Disturb",
            $"{_clr($"{Constants.Inverser}/afk{Constants.Inverser}", ConsoleColor.Green)} : Toggle Away from Keyboard",
            $"{_clr($"{Constants.Inverser}/password{Constants.Inverser}", ConsoleColor.Green)} : Change your password",
            $"{_clr($"{Constants.Inverser}/ghosts{Constants.Inverser}", ConsoleColor.Green)} : See your other sessions and disconnect them",
            $"{Constants.Inverser}--- {_clr("Blurbs (one-liners at login)", ConsoleColor.Yellow)} ---{Constants.Inverser}",
            $"{_clr($"{Constants.Inverser}/blurb{Constants.Inverser}", ConsoleColor.Green)} : Shows a random blurb",
            $"{_clr($"{Constants.Inverser}/blurb" + " {blurb}" + $"{Constants.Inverser}", ConsoleColor.Green)} : Adds a new blurb",
            $"{_clr($"{Constants.Inverser}/blurbs{Constants.Inverser}", ConsoleColor.Green)} : Lists all blurbs",
            $"{_clr($"{Constants.Inverser}/blurbadmin del #{Constants.Inverser}", ConsoleColor.Green)} : Delete a blurb (if yours)",
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
