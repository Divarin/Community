using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Menus
{
    public static class Users
    {
        private static readonly string[] _lines = new[]
        {
            $"{Constants.Inverser}*** Users Help ***{Constants.Inverser}",
            $"{Constants.Spaceholder}",
            $"{Constants.Inverser}/who{Constants.Inverser}".Color(ConsoleColor.Green) + " : List of users currently logged on.",
            $"{Constants.Inverser}/users{Constants.Inverser}".Color(ConsoleColor.Green) + " : List of all users whether currently logged on or not.",
            $"{Constants.Inverser}/kick (username){Constants.Inverser}".Color(ConsoleColor.Green) + " : As a moderator of the channel you can use this to kick the user out of the channel.  If the channel is {Constants.DefaultChannelName} and you are a global moderator or administrator then they will be booted from the system.",
            $"{Constants.Inverser}/ui{Constants.Inverser}".Color(ConsoleColor.Green) + " : User Info, shows info about you.",
            $"{Constants.Inverser}/ui (username){Constants.Inverser}".Color(ConsoleColor.Green) + " : User Info, shows info about the given user.",
            $"{Constants.Inverser}/si{Constants.Inverser}".Color(ConsoleColor.Green) + " : Session Info, shows info about all current sessions (users online right now).",
            $"{Constants.Inverser}/si (username){Constants.Inverser}".Color(ConsoleColor.Green) + " : Session Info, shows info about the session(s) used by the given user (if that user is online right now).",
            $"{Constants.Inverser}/cal{Constants.Inverser}".Color(ConsoleColor.Green) + " : Live-chat calendar.  Find out when specific people will be available to chat in real-time."
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
