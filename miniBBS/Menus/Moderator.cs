using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Menus
{
    public static class Moderator
    {
        private static Func<string, ConsoleColor, string> _clr = (str, clr) => UserIoExtensions.WrapInColor(str, clr);

        //    12345678901234567890123456789012345678901234567890123456789012345678901234567890
        //    1234567890123456789012345678901234567890
        private static readonly string[] _lines = new[]
        {
            $"*** {_clr("Channel Moderator Help", ConsoleColor.Yellow)} ***",
            $"{Constants.Spaceholder}",
            $"{_clr("/ch +i", ConsoleColor.Green)} : Sets the channel to Invite Only.",
            $"{_clr("/ch -i", ConsoleColor.Green)} : Removes the Invite Only flag.",
            $"{_clr("/ch i (username)", ConsoleColor.Green)} : Toggle invite to user (username).",
            $"{_clr("/ch i", ConsoleColor.Green)} : Lists invited users.",
            $"{_clr("/ch m (username)", ConsoleColor.Green)} : Toggles (username) as moderator.",
            $"{_clr("/ch m", ConsoleColor.Green)} : Lists moderators.",
            $"{_clr("/ch kick (username)", ConsoleColor.Green)} : Kicks (username) out of the channel.",
            $"{_clr("/ch del", ConsoleColor.Green)} : Deletes the current channel.",
            $"{_clr("/ch movemsg (ch#) (range)", ConsoleColor.Green)} : Moves one or more messages from current channel to (ch#) (channel name or number).  Use '/ch movemsg' for detailed examples on using this.",
            $"(See '{_clr("/? msgs", ConsoleColor.Green)}' help for info on channel messages, deleting etc...)"
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
