using miniBBS.Core;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Menus
{
    public static class Channels
    {
        private static readonly string[] _lines = new[]
        {
            "*** Channels Help ***",
            $"{Constants.Spaceholder}",
            "/chl : Lists channels that you have access to.",
            "/ch (channel name or number) : Switches to the given channel or, if it is named and it does not exist then creates the channel and sets you as the moderator.",
            string.Format("{0}{0}{0}Examples: ", Constants.Spaceholder),
            string.Format("{0}{0}{0}/ch General    :  switches to the General channel (#1).", Constants.Spaceholder),
            string.Format("{0}{0}{0}/ch FooBar     :  if channel 'FooBar' exists (and you can access it) switches to it.", Constants.Spaceholder),
            string.Format("{0}{0}{0}/ch 42         :  if channel #42 exists (and you can access it) switches to it.", Constants.Spaceholder),
            string.Format("{0}{0}{0}/ch FooBar     :  if channel 'FooBar' does *not* exist, creates it and makes you the moderator.", Constants.Spaceholder),
            $"{Constants.Spaceholder}",
            "** Channel Moderator Commands **",
            "/ch +i : Sets the channel to Invite Only.  Moderators can still join but anyone else will need to be sent an invite.",
            "/ch -i : Removes the Invite Only flag, opening up the channel to everyone.",
            "/ch i (username) : Toggle invite, either grants or removes an invite to the channel to user (username).",
            string.Format("{0}{0}{0}Examples: ", Constants.Spaceholder),
            string.Format("{0}{0}{0}/ch i jimbob    : If jimbob is not invited, then he becomes invited.", Constants.Spaceholder),
            string.Format("{0}{0}{0}/ch i jimbob    : If jimbob is already invited, then he becomes uninvited.", Constants.Spaceholder),
            "/ch i : This command, not including a username, lists all users who have invites to the channel.",
            "/ch m (username) : Toggles whether the user (username) is also a moderator of the channel.",
            "/ch m : This command, not including a username, lists all users who are moderators of this channel.",
            "/ch kick (username)  : Kicks the user (username) out of the channel.",
            $"{Constants.Spaceholder}",
            "/ch del : Deletes the current channel if access permits.",
            "(See '/? msgs' help for info on channel messages, deleting etc...)"
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
