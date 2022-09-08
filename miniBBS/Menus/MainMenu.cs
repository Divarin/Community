using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Menus
{
    public static class MainMenu
    {
        private static Func<string, ConsoleColor, string> _clr = (str, clr) => UserIoExtensions.WrapInColor(str, clr);

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
            $"{_clr("/? channels", ConsoleColor.Green)}, {_clr("/? msgs", ConsoleColor.Green)}, {_clr("/? users", ConsoleColor.Green)}, {_clr("/? bells", ConsoleColor.Green)}, {_clr("/? emotes", ConsoleColor.Green)}, {_clr("/? context", ConsoleColor.Green)}, {_clr("/? mod", ConsoleColor.Green)}, {_clr("/? voice", ConsoleColor.Green)} ",
            //$"/? users    : Info on users",
            //$"/? msgs     : Info on messages",
            //$"/? bells    : Info on bell alerts",
            //$"/? emotes   : Info on emotes",
            $"--- {_clr("Subsystems", ConsoleColor.Yellow)} ---",
            $"{_clr("/cal", ConsoleColor.Green)} : Live-chat events calendar",
            $"{_clr("/mail", ConsoleColor.Green)} : E-Mail",
            $"{_clr("/polls", ConsoleColor.Green)} : Vote on stuff",
            $"{_clr("/files", ConsoleColor.Green)} : Files, Games, BASIC & more",
            //$"{_clr("/polls", ConsoleColor.Green)} : Polls",
            $"--- {_clr("Misc", ConsoleColor.Yellow)} ---",
            $"{_clr("/fauxmain", ConsoleColor.Green)} : Shows the 'new user' faux-main-menu",
            $"{_clr("/newuser", ConsoleColor.Green)} : Read new user docs",
            $"{_clr("/doors", ConsoleColor.Green)} : Finds all user-made BASIC programs and provides a way to quick-launch them.",
            $"{_clr("/term", ConsoleColor.Green)} : Config Terminal",
            $"{_clr("/dnd", ConsoleColor.Green)} : Toggle Do Not Disturb",
            $"{_clr("/afk", ConsoleColor.Green)} : Toggle Away from Keyboard",
            $"{_clr("/password", ConsoleColor.Green)} : Change your password",
            //$"/pp (minutes) : Changes the 'keep alive' (ping pong) timer.  This sends a space followed by a backspace every so often (default {Constants.DefaultPingPongDelayMin} minues).  Some terminals may need this more frequent or may need it disabled.",
            //$"{Constants.Spaceholder.Repeat(2)}/pp 0 : Stops ping/pongs",
            //$"{Constants.Spaceholder.Repeat(2)}/pp 1 : Sets ping/pongs to every 1 minute",
            //$"{Constants.Spaceholder.Repeat(2)}/pp 5 : Sets ping/pongs to every 5 minutes",
            //$"{Constants.Spaceholder.Repeat(2)}/pp 15 : Sets ping/pongs to every 15 minutes (default)",
            //$"{Constants.Spaceholder.Repeat(2)}",
        //    "About the prompt:",            
        //    "(/?=help) (302) <14:17> [1:General] ",
        //    "The prompt has four sections they show, from left to right:",
        //    $"{Constants.Spaceholder.Repeat(2)}Type /? for this menu (pretty self-explainatory)",
        //    $"{Constants.Spaceholder.Repeat(2)}The number of new messages in this channel*",
        //    $"{Constants.Spaceholder.Repeat(2)}The current time (see /tz for timezone information)",
        //    $"{Constants.Spaceholder.Repeat(2)}The current channel number and name",
        //    $"{Constants.Spaceholder.Repeat(3)}* Actual 'new message' count is how many messages are between the current message pointer and the high message number for this channel"
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
