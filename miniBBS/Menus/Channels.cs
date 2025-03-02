using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Menus
{
    public static class Channels
    {
        private static Func<string, ConsoleColor, string> _clr = (str, clr) => UserIoExtensions.WrapInColor(str, clr);

        //    12345678901234567890123456789012345678901234567890123456789012345678901234567890
        //    1234567890123456789012345678901234567890
        private static readonly string[] _lines = new[]
        {
            $"{Constants.Inverser}*** {_clr("Channels Help", ConsoleColor.Yellow)} ***{Constants.Inverser}",
            $"{Constants.Spaceholder}",
            $"{_clr($"{Constants.Inverser}/chl{Constants.Inverser}", ConsoleColor.Green)} : Lists channels.",
            $"{_clr($"{Constants.Inverser}/ch (channel name or number){Constants.Inverser}", ConsoleColor.Green)} : Switch to channel.",
            $"{_clr($"{Constants.Inverser}/ch (new channel name){Constants.Inverser}", ConsoleColor.Green)} : Creates a new channel.",
            $"{_clr($"{Constants.Inverser}[{Constants.Inverser}", ConsoleColor.Green)} : Switch to previous channel.",
            $"{_clr($"{Constants.Inverser}]{Constants.Inverser}", ConsoleColor.Green)} : Switch to next channel.",
            $"See '{_clr($"{Constants.Inverser}/? mod{Constants.Inverser}", ConsoleColor.Green)}' for help on channel moderator commands.",
            $"See '{_clr($"{Constants.Inverser}/? msgs{Constants.Inverser}", ConsoleColor.Green)}' for help for info on channel messages, deleting etc...)"
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
