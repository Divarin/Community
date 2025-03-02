using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Menus
{
    public static class Moderator
    {
        private static Func<string, ConsoleColor, string> _clr = (str, clr) => UserIoExtensions.WrapInColor(str, clr);

        private static readonly string[] _lines = new[]
        {
            $"{Constants.Inverser}*** {_clr("Channel Moderator Help", ConsoleColor.Yellow)} ***{Constants.Inverser}",
            $"{Constants.Spaceholder}",
            $"{_clr($"{Constants.Inverser}/ch +i{Constants.Inverser}", ConsoleColor.Green)} : Sets the channel to Invite Only.",
            $"{_clr($"{Constants.Inverser}/ch -i{Constants.Inverser}", ConsoleColor.Green)} : Removes the Invite Only flag.",
            $"{_clr($"{Constants.Inverser}/ch i (username){Constants.Inverser}", ConsoleColor.Green)} : Toggle invite to user (username).",
            $"{_clr($"{Constants.Inverser}/ch i{Constants.Inverser}", ConsoleColor.Green)} : Lists invited users.",
            $"{_clr($"{Constants.Inverser}/ch m (username){Constants.Inverser}", ConsoleColor.Green)} : Toggles (username) as moderator.",
            $"{_clr($"{Constants.Inverser}/ch m{Constants.Inverser}", ConsoleColor.Green)} : Lists moderators.",
            $"{_clr($"{Constants.Inverser}/ch kick (username){Constants.Inverser}", ConsoleColor.Green)} : Kicks (username) out of the channel.",
            $"{_clr($"{Constants.Inverser}/ch del{Constants.Inverser}", ConsoleColor.Green)} : Deletes the current channel.",
            $"{_clr($"{Constants.Inverser}/ch ren{Constants.Inverser}", ConsoleColor.Green)} : Renames the current channel.",
            $"{_clr($"{Constants.Inverser}/movemsg (ch#) (range){Constants.Inverser}", ConsoleColor.Green)} : Moves one or more messages from current channel to (ch#) (channel name or number).  Use '/movemsg' for detailed examples on using this.",
            $"{_clr($"{Constants.Inverser}/movethread (ch#){Constants.Inverser}", ConsoleColor.Green)} : Moves one or more messages (starting with the last read message, following the thread) from current channel to (ch#) (channel name or number).",
            $"See '{_clr($"{Constants.Inverser}/? msgs{Constants.Inverser}", ConsoleColor.Green)}' help for info on channel messages, deleting etc...",
            $"See '{_clr($"{Constants.Inverser}/? voice{Constants.Inverser}", ConsoleColor.Green)}' help for info on voice: who can talk in the channel.",
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
