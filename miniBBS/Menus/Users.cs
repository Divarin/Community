using miniBBS.Core;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Menus
{
    public static class Users
    {
        private static readonly string[] _lines = new[]
        {
            "*** Users Help ***",
            " ",
            "/who : List of users currently logged on.",
            "/users : List of all users whether currently logged on or not.",
            $"/kick (username) : As a moderator of the channel you can use this to kick the user out of the channel.  If the channel is {Constants.DefaultChannelName} and you are a global moderator or administrator then they will be booted from the system.",
            "/ui  : User Info, shows info about you.",
            "/ui (username) : User Info, shows info about the given user.",
            "/si  : Session Info, shows info about all current sessions (users online right now).",
            "/si (username) : Session Info, shows info about the session(s) used by the given user (if that user is online right now).",
            "/cal : Live-chat calendar.  Find out when specific people will be available to chat in real-time."
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
