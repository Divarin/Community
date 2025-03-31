using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Menus
{
    public static class Emotes
    {
        private static readonly string[] _lines = new[]
        {
            $"{Constants.Inverser}*** {"Emotes".Color(ConsoleColor.Yellow)} ***{Constants.Inverser}",
            $"{Constants.Spaceholder}",
            "Emotes are a way you can get the attention of another user or anyone in the channel without actually posting a message.",
            "Emotes, unlike messages, are not added to the chat history so they cannot be seen by users that log in later, they can " +
            "only be seen by users who are online at the moment and in the same channel as you.",
            $"{Constants.Spaceholder}",
            "At this time there is a limited number of emotes that can be used.  For each of these you can either direct them to " +
            "a particular user or to the channel as a whole.  If they are directed to a user then only you and that user will see " +
            "the emote.",
            $"{Constants.Spaceholder} example: '{Constants.Inverser}{"/wave".Color(ConsoleColor.Green)}{Constants.Inverser}' notifies everyone in the channel: 'Soandso waves to the channel'.",
            $"{Constants.Spaceholder} example: '{Constants.Inverser}{"/wave jimbob".Color(ConsoleColor.Green)}{Constants.Inverser}' notifies only jimbob (if he is online): 'Soandso waves to Jimbob'.",
            $"{Constants.Spaceholder}",
            $"The emotes available at this time are: {"/wave".Color(ConsoleColor.Green)}, {"/smile".Color(ConsoleColor.Green)}, {"/frown".Color(ConsoleColor.Green)}, {"/wink".Color(ConsoleColor.Green)}, {"/nod".Color(ConsoleColor.Green)}, {"/poke".Color(ConsoleColor.Green)}, {"/goodbye".Color(ConsoleColor.Green)}, {"/me".Color(ConsoleColor.Green)}, and {"/online".Color(ConsoleColor.Green)}",
            $"{Constants.Spaceholder}",
            $"{Constants.Inverser}{"/me".Color(ConsoleColor.Green)}{Constants.Inverser} allows you to use an arbitrary emote like '/me dances in the moonlight'.  This is limited to {Constants.MaxInputLength-4} characters."
        };
        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
