using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Menus
{
    public static class Messages
    {
        private static Func<string, ConsoleColor, string> _clr = (str, clr) => UserIoExtensions.WrapInColor(str, clr);


        //  {_clr("", ConsoleColor.Green)}
        //    12345678901234567890123456789012345678901234567890123456789012345678901234567890
        //    1234567890123456789012345678901234567890
        private static readonly string[] _lines = new[]
        {
            $"*** {_clr("Messages Help", ConsoleColor.Yellow)} ***",
            $"{Constants.Spaceholder}",
            $"To post a message simply {_clr("type it", ConsoleColor.Green)} as you would in a chat session.",
            $"While writing your message if you want to abort without backspacing over the whole message you can press {_clr("CTRL-C", ConsoleColor.Green)}.",
            $"{Constants.Spaceholder}",
            $"--- {_clr("Navigation", ConsoleColor.Yellow)} ---",
            $"{_clr("ENTER", ConsoleColor.Green)} : Reads next message.",
            $"{_clr("/ur", ConsoleColor.Green)} : Moves to the first unread message.",
            $"{_clr("/re", ConsoleColor.Green)} : Reads previous message's 're:' message (see '{_clr("/? context", ConsoleColor.Green)}').",
            $"{_clr("/123", ConsoleColor.Green)} : Jump to message # 123.  Example: '/10825' jumps to message 10825.",
            $"{_clr("/e", ConsoleColor.Green)}  : Jumps to end of messages.",
            $"{_clr("/0", ConsoleColor.Green)}  : Jumps to start of messages.",
            $"{_clr("/since (date)", ConsoleColor.Green)}  : Jumps to first message on or after date.",
            $"{_clr("<", ConsoleColor.Green)}, {_clr(">", ConsoleColor.Green)} : Move back or forward (same as {_clr("ENTER", ConsoleColor.Green)}) one message.",
            $"{_clr("[", ConsoleColor.Green)}, {_clr("]", ConsoleColor.Green)} : Move back or forward one channel (see '" + "/? channels".Color(ConsoleColor.Green) + "').",
            $"{_clr("/read", ConsoleColor.Green)} : Begins continuous output of messages starting at current message.  You can abort at page breaks (more prompts).",
            $"{_clr("/archive", ConsoleColor.Green)} : Toggles whether archived (older) messages are shown or hidden (default).",
            $"--- {_clr("Search", ConsoleColor.Yellow)} ---",
            $"{_clr("/pin", ConsoleColor.Green)} : Pins the last message you read. Optional 'p' can be added to make it a 'private' pin: '/pin p'",
            $"{_clr("/pin (msg #)", ConsoleColor.Green)} : Pins the specified message.  Optional 'p' can be added to make it a 'private' pin: '/pin 5051 p'",
            $"{_clr("/pins", ConsoleColor.Green)} : Views pinned messages",
            $"{_clr("/unpin (msg #)", ConsoleColor.Green)} : Removes one of your message pins",
            $"{_clr("/f (keyword)", ConsoleColor.Green)} : Searches for messages containing the keyword (most recent first).",
            $"{_clr("/fs (keyword)", ConsoleColor.Green)} : Searches for messages that start with the keyword (most recent first).",
            $"{_clr("/fu (username)", ConsoleColor.Green)} : Searches for messages from user (most recent first).",
            $"{_clr("/index", ConsoleColor.Green)} : Sub-menu for indexing by certain criteria.",

            $"--- {_clr("Post/Edit", ConsoleColor.Yellow)} ---",
            $"{_clr("/post", ConsoleColor.Green)} : Use the line editor to write a message, this allows for more advanced editing and lets you break up your posts into multiple paragraphs.",
            $"{_clr("/new (message)", ConsoleColor.Green)} : Enters a message in the chat as normal but also omits the 'in response to' (re:) number.",
            $"{_clr("/d", ConsoleColor.Green)} : Delete the last message that you typed.",
            $"{_clr("/d (msg #)", ConsoleColor.Green)} : Deletes the given message number.",
            $"{_clr("/edit \"(search)\" \"(replace)\"", ConsoleColor.Green)} : Edits the last message you typed.",
            $"{_clr("/edit (msg #) \"(search)\" \"(replace)\"", ConsoleColor.Green)} : Edits the specified message.",
            $"{_clr("/rere (n)", ConsoleColor.Green)}: Edits only the 're:' (in-response-to) number of the last message you read/posted",
            $"{_clr("/rere (msg #) (n)", ConsoleColor.Green)} : Edits only the 're:' (in-response-to) number of the specified message",
            $"{_clr("/combine (msg #1) (msg #2)", ConsoleColor.Green)} : Combines two of your messages into one message.",

            $"--- {_clr("Misc", ConsoleColor.Yellow)} ---",
            $"{_clr("/dnd", ConsoleColor.Green)} : Toggle Do Not Disturb mode.",
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
