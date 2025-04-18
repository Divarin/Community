﻿using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Menus
{
    public static class Context
    {
        private static readonly string[] _lines = new[]
        {
            $"{Constants.Inverser}*** {"Context Help".Color(ConsoleColor.Yellow)} ***{Constants.Inverser}",
            $"{Constants.Spaceholder}",
            $"Although {Constants.BbsName} is a chat system modelled after IRC (but with history for non-real-time converstaion) there is still " +
            "a concept of threads.  When someone types something into the chat, the last message that they read before entering their own is attached " +
            $"to their message.  This is shown as '{Constants.Inverser}{"re: 123".Color(ConsoleColor.Magenta)}{Constants.Inverser}' (where 123 is the last message they read before typing).",
            $"{Constants.Spaceholder}",
            "Of course it's possible that this new message is not really in response to message 123 and the user might just be starting up a new topic altogether. " +
            "However, what the 're:' number does for you is tell you \"If this message is in response to anything then it is most likely in response to message # 123\".",
            $"{Constants.Spaceholder}",
            $"Side note: See '{Constants.Inverser}{"/? msgs".Color(ConsoleColor.Green)}{Constants.Inverser}' command on '{Constants.Inverser}{"/new".Color(ConsoleColor.Green)}{Constants.Inverser}' for information about starting a new topic.",
            $"{Constants.Spaceholder}",
            $"For example, let's say you just read message number 1003 and that message shows '{Constants.Inverser}{"re:987".Color(ConsoleColor.Magenta)}{Constants.Inverser}' (so 1003 is 'in response to' # 987).  If you need context for 1003 " +
            "then you could manually switch to 987 ('/987' -> enter) read it, then move back to 1004 to read the next message ('/1004' -> enter). " +
            "However this is a bit tedious especially if you have to go back further and provide context for message 987.",
            $"{Constants.Spaceholder}",
            $"This is where the Context command ({Constants.Inverser}{"/re".Color(ConsoleColor.Green)}{Constants.Inverser}) comes in.  It allows you to trace back the thread of conversation by following the breadcrumbs (re: numbers) of each " +
            "message.  Using the example above, if you just read 1003 (so your message pointer is now at 1004 and will be the next message to be shown when you press enter) " +
            $"you can type '{Constants.Inverser}{"/re".Color(ConsoleColor.Green)}{Constants.Inverser}' and message # 987 will show up but your message pointer will not change.",
            $"{Constants.Spaceholder}",
            "In addition, until you actually press enter to read message 1004, you can use the /re command again to view the message that 987 is 'in response to'.  You can " +
            "continue to use /re to follow the conversation thread all the way back until you hit a message that has no 're:' number.",
            $"{Constants.Spaceholder}",
            "One issue though is that if a message in the thread was deleted then you will not be able to use /re to follow the thread back beyond that point.",
            $"{Constants.Spaceholder}",
            $"{Constants.Inverser}Optional parameters to pass to /re command:{Constants.Inverser}",
            $"{Constants.Inverser}{"/re 0".Color(ConsoleColor.Green)}{Constants.Inverser} : Recursively search for the first message in the thread.",
            $"{Constants.Inverser}{"/re e".Color(ConsoleColor.Green)}{Constants.Inverser} : Recursively search for the last message in the thread.",
            $"{Constants.Inverser}{"/re >".Color(ConsoleColor.Green)}{Constants.Inverser} : Advance forward to the next message in the thread.",
            $"{Constants.Inverser}{"/ra".Color(ConsoleColor.Green)}{Constants.Inverser}   : Same as '/re >'.",
            $"NOTE  : With '{Constants.Inverser}{"/re >".Color(ConsoleColor.Green)}{Constants.Inverser}' (and '{Constants.Inverser}{"/ra".Color(ConsoleColor.Green)}{Constants.Inverser}') if there is more than one response then snippets of each response is shown and you're asked which branch you'd like to follow"
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
