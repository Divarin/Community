using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions_UserIo;
using System;

namespace miniBBS.Menus
{
    public static class Voice
    {
        private static Func<string, ConsoleColor, string> _clr = (str, clr) => UserIoExtensions.WrapInColor(str, clr);

        //    12345678901234567890123456789012345678901234567890123456789012345678901234567890
        //    1234567890123456789012345678901234567890
        private static readonly string[] _lines = new[]
        {
            $"*** {_clr("Channels Voice Help", ConsoleColor.Yellow)} ***",
            $"{Constants.Spaceholder}",
            $"{_clr("/ch +v", ConsoleColor.Green)} : Sets flag: channel requires voice to post.",
            $"{_clr("/ch -v", ConsoleColor.Green)} : Removes flag: channel requires voice to post.",
            $"{_clr("/ch +v (username)", ConsoleColor.Green)} : Gives voice to (username).",
            $"{_clr("/ch -v (username)", ConsoleColor.Green)} : Revokes voice to (username).",
            $"{_clr("/ch vlist", ConsoleColor.Green)} : Lists all users with voice.",
            $"{_clr("/ch vall", ConsoleColor.Green)} : Gives voice to everyone currently in the channel.",
            $"{_clr("/ch vnone", ConsoleColor.Green)} : Revokes voice from everyone.",
            $" --- {_clr("Voice Request Queue", ConsoleColor.Yellow)} ---",
            $"The Voice Request Queue is intended to be used during live Q&A sessions, it cannot be used with offline users.",
            $"{_clr("/ch vq", ConsoleColor.Green)} : Shows the current queue.",
            $"{_clr("/ch vq (duration)", ConsoleColor.Green)} : Starts a new Voice Request Queue for the channel which will expire after (duration) minutes.",
            $"{_clr("/ch vq +", ConsoleColor.Green)} : Gives voice to the next person in the queue.",
            $"{_clr("/ch vq -", ConsoleColor.Green)} : Skips the next person in the queue.",
            $"{_clr("/ch extend (duration)", ConsoleColor.Green)} : Extends the expiry time for the queue by (duration) minutes.",
            $"{_clr("/ch vq end", ConsoleColor.Green)} : Expires the voice request queue early.",
            $"Note: Users can use these commands to request voice: {_clr("/hand", ConsoleColor.Green)}, {_clr("/raise", ConsoleColor.Green)}, {_clr("/raisehand", ConsoleColor.Green)}, {_clr("/voice", ConsoleColor.Green)}.",
            "If a queue is active they will be enqueued, otherwise moderators will just see a notification that the user wants voice."
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
