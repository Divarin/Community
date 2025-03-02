using miniBBS.Core;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Menus
{
    public static class About
    {
        private static readonly string[] _lines = new[]
        {
            $"{Constants.Inverser}*** About Community ***{Constants.Inverser}",
            $"Version: {Constants.Version}",
            $"{Constants.Spaceholder}",
            "Mutiny Community is a new kind of Bulletin Board System (BBS).  " +
            "The lifeblood of any BBS is the message base.  This is where people " +
            "come together and discuss various topics.  The usual format for a BBS's " +
            "message base area is similar to a forum.  ",
            $"{Constants.Spaceholder}",
            "Here at Mutiny Community the format is differnet.  It's more like a chat session " +
            "not unlike Internet Relay Chat (IRC) or other real-time chat systems.  However, " +
            "there is a drawback to the chat format, that is you only see what has been said since " +
            "you logged on and once you log off you are missing out on the conversation.",
            $"{Constants.Spaceholder}",
            "So Mutiny Community combines these two formats (Forums and Chat) and turns it into a " +
            "multi-user SMS like session.  You can read the entire backlog of 'chats' as well as " +
            "see anything that was said between when you last logged off and now.",
            $"{Constants.Spaceholder}",
            "Mutiny BBS (mutinybbs.com:2332) offers Forum style message boards, games, text files, " +
            "file transfers, and the other usual BBSy things.  But here at Community it's purely chat."
        };

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(string.Join(Environment.NewLine, _lines));
        }
    }
}
