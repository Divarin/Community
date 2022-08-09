using miniBBS.Core;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Menus
{
    public static class MainMenu
    {
        private static readonly string[] _lines = new[]
        {
            "*** Community Main Menu ***",
            $"{Constants.Spaceholder}",
            "All commands start with a forward slash (/).  Any line you enter that does not start with this will be entered into the chat log for the active channel.",
            $"{Constants.Spaceholder}",
            "ENTER : Show the next message.",
            $"{Constants.Spaceholder}",
            "List of slash commands:",
            "/? : Shows this menu.",
            "/a : About this sytem.",
            "/o : Logoff.",
            "/term : Setup terminal emulation parameters (rows, cols, ascii, ansi, cbm)",
            "/newuser : Read new user documentation.",
            "/? channels : Info on channels, changing to, listing, creating, etc...",
            "/? users    : Info on users, who is online, who is offline, etc...",
            "/? msgs     : Info on messages, navigating, searching, etc...",
            "/? bells    : Info on bell (audible alerts)",
            "/? emotes   : Info on emotes",
            "/cal : View the live-chat events calendar.",
            "/dnd : Toggle Do Not Disturb mode.  In this mode you will not receive notifications from activity on other nodes.",
            "/afk : Toggle Away from Keyboard flag, lets others know you might not respond.",
            "/afk mowing lawn : Optionally, you can provide a reason you're away from keyboard.",
            "/mail : Lists options for the mail subsystem",
            "/text : TextFiles Browser subsystem",
            "/password : Utility to update your password",
            "/fauxmain : Shows the 'new user' faux-main-menu",
            $"/pp (minutes) : Changes the 'keep alive' (ping pong) timer.  This sends a space followed by a backspace every so often (default {Constants.DefaultPingPongDelayMin} minues).  Some terminals may need this more frequent or may need it disabled.",
            $"{Constants.Spaceholder}{Constants.Spaceholder}/pp 0 : Stops ping/pongs",
            $"{Constants.Spaceholder}{Constants.Spaceholder}/pp 1 : Sets ping/pongs to every 1 minute",
            $"{Constants.Spaceholder}{Constants.Spaceholder}/pp 5 : Sets ping/pongs to every 5 minutes",
            $"{Constants.Spaceholder}{Constants.Spaceholder}/pp 15 : Sets ping/pongs to every 15 minutes (default)",
            $"{Constants.Spaceholder}",
            "About the prompt:",            
            "(/?=help) (302) <14:17> [1:General] ",
            "The prompt has four sections they show, from left to right:",
            $"{Constants.Spaceholder}{Constants.Spaceholder}Type /? for this menu (pretty self-explainatory)",
            $"{Constants.Spaceholder}{Constants.Spaceholder}The number of new messages in this channel*",
            $"{Constants.Spaceholder}{Constants.Spaceholder}The current time (see /tz for timezone information)",
            $"{Constants.Spaceholder}{Constants.Spaceholder}The current channel number and name",
            $"{Constants.Spaceholder}{Constants.Spaceholder}{Constants.Spaceholder}* Actual 'new message' count is how many messages are between the current message pointer and the high message number for this channel"
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
