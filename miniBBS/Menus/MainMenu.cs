using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Menus
{
    public static class MainMenu
    {
        private static readonly Func<string, ConsoleColor, string> _clr = (str, clr) => UserIoExtensions.WrapInColor(str, clr);

        private static readonly string[] _lines = new[]
        {
            $"{Constants.Inverser}*** {_clr($"{Constants.BbsName} Chat Menu", ConsoleColor.Yellow)} ***{Constants.Inverser}",
            $"{_clr("Commands start with a forward slash (/)", ConsoleColor.Cyan)}",
            $"{Constants.Spaceholder}",
            $"{_clr($"{Constants.Inverser}ENTER{Constants.Inverser}", ConsoleColor.Green)} : Show the next message",
            "To post or respond to one, just type!",
            $"{_clr($"{Constants.Inverser}/a{Constants.Inverser}", ConsoleColor.Green)} : About this sytem",
            $"{_clr($"{Constants.Inverser}/main{Constants.Inverser}", ConsoleColor.Green)} : Go to the Main Menu",
            $"{_clr($"{Constants.Inverser}/o{Constants.Inverser}", ConsoleColor.Green)} : Logoff",
            $"{_clr($"{Constants.Inverser}/who{Constants.Inverser}", ConsoleColor.Green)} : List of users currently logged on",
            $"{_clr($"{Constants.Inverser}/chl{Constants.Inverser}", ConsoleColor.Green)} : List chat channels",
            $"{Constants.Inverser}--- {_clr("More Help Menus", ConsoleColor.Yellow)} ---{Constants.Inverser}",
                $"{_clr($"{Constants.Inverser}/?{Constants.Inverser} channels", ConsoleColor.Green)}, " + 
                $"{_clr($"{Constants.Inverser}/?{Constants.Inverser} msgs", ConsoleColor.Green)}, " +
                $"{_clr($"{Constants.Inverser}/?{Constants.Inverser} more", ConsoleColor.Green)}, " +
                $"{_clr($"{Constants.Inverser}/?{Constants.Inverser} users", ConsoleColor.Green)}, " + 
                $"{_clr($"{Constants.Inverser}/?{Constants.Inverser} bells", ConsoleColor.Green)}, " + 
                $"{_clr($"{Constants.Inverser}/?{Constants.Inverser} emotes", ConsoleColor.Green)}, " + 
                $"{_clr($"{Constants.Inverser}/?{Constants.Inverser} context", ConsoleColor.Green)}, " + 
                $"{_clr($"{Constants.Inverser}/?{Constants.Inverser} mod", ConsoleColor.Green)}, " + 
                $"{_clr($"{Constants.Inverser}/?{Constants.Inverser} voice", ConsoleColor.Green)}, " +
                $"{_clr($"{Constants.Inverser}/?{Constants.Inverser} misc", ConsoleColor.Green)} ",
            $"{Constants.Inverser}--- {_clr("Subsystems", ConsoleColor.Yellow)} ---{Constants.Inverser}",
            //$"{_clr("/msg", ConsoleColor.Green)} : Message Base view",
            $"{_clr($"{Constants.Inverser}/b{Constants.Inverser}", ConsoleColor.Green)} : Bulletin Boards",
            $"{_clr($"{Constants.Inverser}/cal{Constants.Inverser}", ConsoleColor.Green)} : Live-chat events calendar",
            $"{_clr($"{Constants.Inverser}/mail{Constants.Inverser}", ConsoleColor.Green)} : E-Mail",
            $"{_clr($"{Constants.Inverser}/polls{Constants.Inverser}", ConsoleColor.Green)} : Vote on stuff",
            $"{_clr($"{Constants.Inverser}/files{Constants.Inverser}", ConsoleColor.Green)} : Files, Games, BASIC & more",
            $"{_clr($"{Constants.Inverser}/gopher{Constants.Inverser}", ConsoleColor.Green)} : Explore Gopherspace!",
            $"{_clr($"{Constants.Inverser}/nullspace{Constants.Inverser}", ConsoleColor.Green)} : Enter NullSpace Chat",
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
