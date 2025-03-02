using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Menus
{
    public static class Messages
    {
        private static Func<string, ConsoleColor, string> _clr = (str, clr) => UserIoExtensions.WrapInColor(str, clr);

        private static readonly string[] _lines = new[]
        {
            $"{Constants.Inverser}*** {_clr("Messages Help", ConsoleColor.Yellow)} ***{Constants.Inverser}",
            $"{Constants.Spaceholder}",
            $"To post a message simply {_clr("type it", ConsoleColor.Green)} as you would in a chat session.",
            $"While writing your message if you want to abort without backspacing over the whole message you can press {_clr("CTRL-C", ConsoleColor.Green)}.",
            $"{Constants.Spaceholder}",
            $"{Constants.Inverser}--- {_clr("Navigation", ConsoleColor.Yellow)} ---{Constants.Inverser}",
            $"{_clr($"{Constants.Inverser}ENTER{Constants.Inverser}", ConsoleColor.Green)} : Reads next message.",
            $"{_clr($"{Constants.Inverser}/ur{Constants.Inverser}", ConsoleColor.Green)} : Moves to the first unread message.",
            $"{_clr($"{Constants.Inverser}/re{Constants.Inverser}", ConsoleColor.Green)} : Reads previous message's 're:' message (see '{_clr("/? context", ConsoleColor.Green)}').",
            $"{_clr($"{Constants.Inverser}/123{Constants.Inverser}", ConsoleColor.Green)} : Jump to message # 123.  Example: '/10825' jumps to message 10825.",
            $"{_clr($"{Constants.Inverser}/e{Constants.Inverser}", ConsoleColor.Green)}  : Jumps to end of messages.",
            $"{_clr($"{Constants.Inverser}/0{Constants.Inverser}", ConsoleColor.Green)}  : Jumps to start of messages.",
            $"{_clr($"{Constants.Inverser}/since (date){Constants.Inverser}", ConsoleColor.Green)}  : Jumps to first message on or after date.",
            $"{_clr($"{Constants.Inverser}<{Constants.Inverser}", ConsoleColor.Green)}, {_clr($"{Constants.Inverser}>{Constants.Inverser}", ConsoleColor.Green)} : Move back or forward (same as {_clr("ENTER", ConsoleColor.Green)}) one message.",
            $"{_clr($"{Constants.Inverser}[{Constants.Inverser}", ConsoleColor.Green)}, {_clr($"{Constants.Inverser}]{Constants.Inverser}", ConsoleColor.Green)} : Move back or forward one channel (see '" + "/? channels".Color(ConsoleColor.Green) + "').",
            $"{_clr($"{Constants.Inverser}/read{Constants.Inverser}", ConsoleColor.Green)} : Begins continuous output of messages starting at current message.  You can abort at page breaks (more prompts).",
            $"{_clr($"{Constants.Inverser}/archive{Constants.Inverser}", ConsoleColor.Green)} : Toggles whether archived (older) messages are shown or hidden (default).",
            $"{Constants.Inverser}--- {_clr("Search", ConsoleColor.Yellow)} ---{Constants.Inverser}",
            $"{_clr($"{Constants.Inverser}/pin{Constants.Inverser}", ConsoleColor.Green)} : Pins the last message you read. Optional 'p' can be added to make it a 'private' pin: '/pin p'",
            $"{_clr($"{Constants.Inverser}/pin (msg #){Constants.Inverser}", ConsoleColor.Green)} : Pins the specified message.  Optional 'p' can be added to make it a 'private' pin: '/pin 5051 p'",
            $"{_clr($"{Constants.Inverser}/pins{Constants.Inverser}", ConsoleColor.Green)} : Views pinned messages",
            $"{_clr($"{Constants.Inverser}/unpin (msg #){Constants.Inverser}", ConsoleColor.Green)} : Removes one of your message pins",
            $"{_clr($"{Constants.Inverser}/f (keyword){Constants.Inverser}", ConsoleColor.Green)} : Searches for messages containing the keyword (most recent first).",
            $"{_clr($"{Constants.Inverser}/fs (keyword){Constants.Inverser}", ConsoleColor.Green)} : Searches for messages that start with the keyword (most recent first).",
            $"{_clr($"{Constants.Inverser}/fu (username){Constants.Inverser}", ConsoleColor.Green)} : Searches for messages from user (most recent first).",
            $"{_clr($"{Constants.Inverser}/index{Constants.Inverser}", ConsoleColor.Green)} : Sub-menu for indexing by certain criteria.",

            $"{Constants.Inverser}--- {_clr("Post/Edit", ConsoleColor.Yellow)} ---{Constants.Inverser}",
            $"{_clr($"{Constants.Inverser}/post{Constants.Inverser}", ConsoleColor.Green)} : Use the line editor to write a message, this allows for more advanced editing and lets you break up your posts into multiple paragraphs.",
            $"{_clr($"{Constants.Inverser}/new (message){Constants.Inverser}", ConsoleColor.Green)} : Enters a message in the chat as normal but also omits the 'in response to' (re:) number.",
            $"{_clr($"{Constants.Inverser}/d{Constants.Inverser}", ConsoleColor.Green)} : Delete the last message that you typed.",
            $"{_clr($"{Constants.Inverser}/d (msg #){Constants.Inverser}", ConsoleColor.Green)} : Deletes the given message number.",
            $"{_clr($"{Constants.Inverser}/edit \"(search)\" \"(replace)\"{Constants.Inverser}", ConsoleColor.Green)} : Edits the last message you typed.",
            $"{_clr($"{Constants.Inverser}/edit (msg #) \"(search)\" \"(replace)\"{Constants.Inverser}", ConsoleColor.Green)} : Edits the specified message.",
            $"{_clr($"{Constants.Inverser}/rere (n){Constants.Inverser}", ConsoleColor.Green)}: Edits only the 're:' (in-response-to) number of the last message you read/posted",
            $"{_clr($"{Constants.Inverser}/rere (msg #) (n){Constants.Inverser}", ConsoleColor.Green)} : Edits only the 're:' (in-response-to) number of the specified message",
            $"{_clr($"{Constants.Inverser}/combine (msg #1) (msg #2){Constants.Inverser}", ConsoleColor.Green)} : Combines two of your messages into one message.",

            $"{Constants.Inverser}--- {_clr("Misc", ConsoleColor.Yellow)} ---{Constants.Inverser}",
            $"{_clr($"{Constants.Inverser}/dnd{Constants.Inverser}", ConsoleColor.Green)} : Toggle Do Not Disturb mode.",
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
