﻿using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Menus
{
    public static class MainMenu
    {
        private static readonly Func<string, ConsoleColor, string> _clr = (str, clr) => UserIoExtensions.WrapInColor(str, clr);

        //    12345678901234567890123456789012345678901234567890123456789012345678901234567890
        //    1234567890123456789012345678901234567890
        private static readonly string[] _lines = new[]
        {
            $"*** {_clr("Community Main Menu", ConsoleColor.Yellow)} ***",
            $"{_clr("Commands start with a forward slash (/)", ConsoleColor.Cyan)}",
            $"{Constants.Spaceholder}",
            $"{_clr("ENTER", ConsoleColor.Green)} : Show the next message",
            "To post or respond to one, just type!",
            $"{_clr("/a", ConsoleColor.Green)} : About this sytem",
            $"{_clr("/o", ConsoleColor.Green)} : Logoff",
            $"{_clr("/who", ConsoleColor.Green)} : List of users currently logged on",
            $"--- {_clr("More Help Menus", ConsoleColor.Yellow)} ---",
                $"{_clr("/? channels", ConsoleColor.Green)}, " + 
                $"{_clr("/? msgs", ConsoleColor.Green)}, " +
                $"{_clr("/? users", ConsoleColor.Green)}, " + 
                $"{_clr("/? bells", ConsoleColor.Green)}, " + 
                $"{_clr("/? emotes", ConsoleColor.Green)}, " + 
                $"{_clr("/? context", ConsoleColor.Green)}, " + 
                $"{_clr("/? mod", ConsoleColor.Green)}, " + 
                $"{_clr("/? voice", ConsoleColor.Green)}, " +
                $"{_clr("/? misc", ConsoleColor.Green)} ",
            $"--- {_clr("Subsystems", ConsoleColor.Yellow)} ---",
            //$"{_clr("/msg", ConsoleColor.Green)} : Message Base view",
            $"{_clr("/b", ConsoleColor.Green)} : Bulletin Boards",
            $"{_clr("/cal", ConsoleColor.Green)} : Live-chat events calendar",
            $"{_clr("/mail", ConsoleColor.Green)} : E-Mail",
            $"{_clr("/polls", ConsoleColor.Green)} : Vote on stuff",
            $"{_clr("/files", ConsoleColor.Green)} : Files, Games, BASIC & more",
            $"{_clr("/gopher", ConsoleColor.Green)} : Explore Gopherspace!",
            $"{_clr("/nullspace", ConsoleColor.Green)} : Enter NullSpace Chat",
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
