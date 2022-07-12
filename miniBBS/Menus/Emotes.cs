using miniBBS.Core;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Menus
{
    public static class Emotes
    {
        private static readonly string[] _lines = new[]
        {
            "*** Emotes ***",
            $"{Constants.Spaceholder}",
            "Emotes are a way you can get the attention of another user or anyone in the channel without actually posting a message.",
            "Emotes, unlike messages, are not added to the chat history so they cannot be seen by users that log in later, they can " +
            "only be seen by users who are online at the moment and in the same channel as you.",
            $"{Constants.Spaceholder}",
            "At this time there is a limited number of emotes that can be used.  For each of these you can either direct them to " +
            "a particular user or to the channel as a whole.  If they are directed to a user then only you and that user will see " +
            "the emote.",
            $"{Constants.Spaceholder} example: '/wave' notifies everyone in the channel: 'Soandso waves to the channel'.",
            $"{Constants.Spaceholder} example: '/wave jimbob' notifies only jimbob (if he is online): 'Soandso waves to Jimbob'.",
            $"{Constants.Spaceholder}",
            "The emotes available at this time are: /wave, /smile, /frown, /wink, /nod, /poke, /goodbye"
        };
        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
