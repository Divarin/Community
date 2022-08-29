﻿using miniBBS.Core;
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
            $"{_clr("/dnd", ConsoleColor.Green)} : Toggle Do Not Disturb mode.",
            $"{_clr("/d", ConsoleColor.Green)} : Delete the last message that you typed.",
            $"{_clr("/d (msg #)", ConsoleColor.Green)} : Deletes the given message number.",
            $"{_clr("/edit \"(search)\" \"(replace)\"", ConsoleColor.Green)} : Edits the last message you typed.",
            $"{_clr("/edit (msg #) \"(search)\" \"(replace)\"", ConsoleColor.Green)} : Edits the specified message.",
            $"{_clr("/re", ConsoleColor.Green)} : Reads previous message's 're:' message (see '{_clr("/? context", ConsoleColor.Green)}').",
            $"{_clr("/rere (n)", ConsoleColor.Green)}: Edits only the 're:' (in-response-to) number of the last message you read/posted",
            $"{_clr("/rere (msg #) (n)", ConsoleColor.Green)} : Edits only the 're:' (in-response-to) number of the specified message",
            $"{_clr("/new (message)", ConsoleColor.Green)} : Enters a message in the chat as normal but also omits the 'in response to' (re:) number.",
            $"{_clr("/123", ConsoleColor.Green)} : Jump to message # 123.  Example: '/10825' jumps to message 10825.",
            $"{_clr("<", ConsoleColor.Green)}, {_clr(">", ConsoleColor.Green)} : Move back or forward (same as {_clr("ENTER", ConsoleColor.Green)}) one message.",
            $"{_clr("/e", ConsoleColor.Green)}  : Jumps to end of messages.",
            $"{_clr("/0", ConsoleColor.Green)}  : Jumps to start of messages.",
            $"{_clr("/pin", ConsoleColor.Green)} : Pins the last message you read. Optional 'p' can be added to make it a 'private' pin: '/pin p'",
            $"{_clr("/pin (msg #)", ConsoleColor.Green)} : Pins the specified message.  Optional 'p' can be added to make it a 'private' pin: '/pin 5051 p'",
            $"{_clr("/pins", ConsoleColor.Green)} : Views pinned messages",
            $"{_clr("/unpin (msg #)", ConsoleColor.Green)} : Removes one of your message pins",
            $"{_clr("/f (keyword)", ConsoleColor.Green)} : Searches for messages containing the keyword (most recent first).",
            $"{_clr("/fs (keyword)", ConsoleColor.Green)} : Searches for messages that start with the keyword (most recent first).",
            $"{_clr("/fu (username)", ConsoleColor.Green)} : Searches for messages from user (most recent first).",
            $"{_clr("/index date", ConsoleColor.Green)} : Shows a snippet of the first message of each day, most recent first.",
            $"{_clr("/index new", ConsoleColor.Green)}  : Shows a snippet of each 'new' message thread (messages not in response to any other message).",
            $"{_clr("/index length", ConsoleColor.Green)} : Shows a snippet of the top 100 longest messages sorted from longest to shortest.",
            $"{_clr("/index links", ConsoleColor.Green)} : Shows a snippet of the most recent 100 messages which contain a link to a text file",
            $"{_clr("/read", ConsoleColor.Green)} : Begins continuous output of messages starting at the message pointer.  You can abort at page breaks (more prompts)."            
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
