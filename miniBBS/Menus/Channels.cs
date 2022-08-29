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
            $"*** {_clr("Channels Help", ConsoleColor.Yellow)} ***",
            $"{Constants.Spaceholder}",
            $"{_clr("/chl", ConsoleColor.Green)} : Lists channels.",
            $"{_clr("/ch (channel name or number)", ConsoleColor.Green)} : Switch to channel.",
            $"{_clr("/ch (new channel name)", ConsoleColor.Green)} : Creates a new channel.",
            $"{_clr("[", ConsoleColor.Green)} : Switch to previous channel.",
            $"{_clr("]", ConsoleColor.Green)} : Switch to next channel.",
            //string.Format("{0}{0}{0}/ch General    :  switches to the General channel (#1).", Constants.Spaceholder),
            //string.Format("{0}{0}{0}/ch FooBar     :  if channel 'FooBar' exists (and you can access it) switches to it.", Constants.Spaceholder),
            //string.Format("{0}{0}{0}/ch 42         :  if channel #42 exists (and you can access it) switches to it.", Constants.Spaceholder),
            //string.Format("{0}{0}{0}/ch FooBar     :  if channel 'FooBar' does *not* exist, creates it and makes you the moderator.", Constants.Spaceholder),
            $"--- {_clr("Channel Moderator Commands", ConsoleColor.Yellow)} ---",
            $"{_clr("/ch +i", ConsoleColor.Green)} : Sets the channel to Invite Only.",
            $"{_clr("/ch -i", ConsoleColor.Green)} : Removes the Invite Only flag.",
            $"{_clr("/ch i (username)", ConsoleColor.Green)} : Toggle invite to user (username).",
            //string.Format("{0}{0}{0}Examples: ", Constants.Spaceholder),
            //string.Format("{0}{0}{0}/ch i jimbob    : If jimbob is not invited, then he becomes invited.", Constants.Spaceholder),
            //string.Format("{0}{0}{0}/ch i jimbob    : If jimbob is already invited, then he becomes uninvited.", Constants.Spaceholder),
            $"{_clr("/ch i", ConsoleColor.Green)} : Lists invited users.",
            $"{_clr("/ch m (username)", ConsoleColor.Green)} : Toggles (username) as moderator.",
            $"{_clr("/ch m", ConsoleColor.Green)} : Lists moderators.",
            $"{_clr("/ch kick (username)", ConsoleColor.Green)} : Kicks (username) out of the channel.",
            $"{_clr("/ch del", ConsoleColor.Green)} : Deletes the current channel.",
            $"(See '{_clr("/? msgs", ConsoleColor.Green)}' help for info on channel messages, deleting etc...)"
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
