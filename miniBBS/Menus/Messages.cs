using miniBBS.Core;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Menus
{
    public static class Messages
    {
        private static readonly string[] _lines = new[]
        {
            "*** Messages Help ***",
            $"{Constants.Spaceholder}",
            "To post a message in the current channel simply type it as you would in a chat session.  " +
            "As long as the line you enter does not start with a slash (/) it will be entered into the " +
            "chat log and made immediately visible to other users who are currently online and in the current channel.",
            "While writing your message if you want to abort without backspacing over the whole message you can press CTRL-C.",
            $"{Constants.Spaceholder}",
            "/dnd : Toggle Do Not Disturb mode.  In this mode you will not receive notifications from activity on other nodes.  This can be helpful while reading a backlog of messages.",
            $"/d : Delete the last message that you typed if it is not more than {Constants.MinutesUntilMessageIsUndeletable} minutes old.",
            $"/d (msg #) : Deletes the given message number if you are a moderator or if it's your's and not more than {Constants.MinutesUntilMessageIsUndeletable} minutes old.",
            $"/edit \"(search)\" \"(replace)\" : Edits the last message you typed to replace the text (search) with (replace).  There must not be spaces within the (search) & (replace) as space delimits the search from the replace.  However you can use dashes as a place holder for spaces.  For example '/edit I'm I-Am' will replace 'I'm' with 'I Am'",
            $"/edit (msg #) \"(search)\" \"(replace)\" : Same as '/edit' above but referring to a specific message number.  Edit follows the same rules as Delete, it must be your message, or you must be a moderator/admin, or it must be a fairly recently entered message.",
            $"/typo : Same as /edit.  Also note with /edit and /typo you can delimit the (search) from (replace) using a single slash (/).  This allows you to use spaces in the (search) & (replace) but the entire line must contain exactly one slash, there cannot be any slashes within the (search) or (replace).",
            "/123 : Jump to message # 123.  Example: '/10825' jumps to message 10825.",
            "/e  : Jumps to end (high message number in this channel).",
            "/0  : Jumps to start (lowest message number in this channel).",
            "/ctx : Reads previous message's 're:' message without altering message pointer.  Type '/? context' for detailed information about this feature.",
            "/new (message) : Enters a message in the chat as normal but also omits the 'in response to' (re:) number.  " + 
            "This is a way to explicitly signify that this message is a new topic and not in response to the last message you read",
            "/f (keyword) : Searches for messages containing the keyword (most recent first).",
            "/fs (keyword) : Searches for messages that start with the keyword (most recent first).",
            "/fu (username) : Searches for messages from user (most recent first).",
            "/index date : Shows a snippet of the first message of each day, most recent first.",
            "/index new  : Shows a snippet of each 'new' message thread (messages not in response to any other message).",
            "/index length : Shows a snippet of the top 100 longest messages sorted from longest to shortest.",
            "/index links : Shows a snippet of the most recent 100 messages which contain a link to a text file",
            "/read : Begins continuous output of messages starting at the message pointer.  You can abort at page breaks (more prompts)."            
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
