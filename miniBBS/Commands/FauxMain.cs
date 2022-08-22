using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Commands
{
    public static class FauxMain
    {
        private static Func<string, ConsoleColor, string> _clr = (txt, clr) => UserIoExtensions.WrapInColor(txt, clr);

        private static readonly string[] _menu = new[]
        {
            $"{_clr("***", ConsoleColor.Yellow)} {_clr("Mutiny Community Faux-Main Menu", ConsoleColor.Magenta)} {_clr("***", ConsoleColor.Yellow)} ",
            $"{_clr("M", ConsoleColor.Green)}) Message Boards",
            $"{_clr("C", ConsoleColor.Green)}) Chat Rooms",
            $"{_clr("T", ConsoleColor.Green)}) Text Files",
            $"{_clr("G", ConsoleColor.Green)}) Games",
            $"{_clr("D", ConsoleColor.Green)}) Downloads",
            $"{_clr("O", ConsoleColor.Green)}) Logoff",
            $" {_clr("---", ConsoleColor.DarkGray)} ",
            $"{_clr("L", ConsoleColor.Green)}) Let's go to the BBS!"
        };

        private static readonly string _messageboards =
            "Okay so here's the deal.  This isn't a 'normal' BBS and although there certainly are messages here " +
            "things aren't going to act and feel like you're expecting.  In-fact this whole 'main menu' is a sham. " +
            $"{Environment.NewLine}" +
            $"There is no main menu, really.  So you want to read the messages?  Here's how you do it.  {Environment.NewLine}" +
            "First,\"(L)et's go to the BBS!\" to get out of this faux-main-menu.  From there you'll end up in a chat room that will " +
            "look a lot like IRC, or MRC, or any other real-time chat system you've ever used.  Except here's the thing, the chat rooms *are* the " +
            "message boards because although people can chat back and forth in real time you can also read all of the chats left before you logged " +
            $"in which means that it acts like a message board as well. {Environment.NewLine}" +
            $"{Environment.NewLine}" +
            "So chat rooms are really the 'main menu' and everywhere you can go on this BBS originates there.  All commands start with a slash " +
            $"so for example '/files' will take you to the text files subsection and '/mail' will take you to the email subsection. {Environment.NewLine}" +
            $"{Environment.NewLine}" +
            "To read the messages all you have to do is keep pressing enter to advance to the next message.  If you want to respond just type a response and " +
            "will automatically be flagged as being 'in-response-to' whatever message you just read.  It may seem a little strange responding to a 'chat message' " +
            "left months or years ago but don't worry that's how things roll here on Community conversations drift back and forth between real-time and not." +
            $"{Environment.NewLine}{Environment.NewLine}" +
            "Be sure to check out the '/?' command to see what all commands you can use and what they do.";

        private static readonly string _textfiles =
            "Okay so here's the deal.  There is a pretty extensive text files subsection on this BBS which includes a mirror of the Jason Scott " +
            "textfiles.com archive as well as the ability for users such as yourself to write up your own text files and even allows for " +
            "multiple users to collaborate together on writing projects.  But, in order to get to this you must first press L for " +
            "\"(L)et's go to the BBS!\" and move beyond this faux-main-menu.  You see this isn't really a main menu, in-fact this BBS doesn't have a main menu. " +
            "It's a chat system with history meaning that the chat room doubles as the message base.  So really you're supposed to 'land' in the " +
            "chat rooms when you log in.  This so-called 'main menu' is just being shown because you're new here and people *expect* BBSs to work " +
            "in a certain way and get flustered when they don't.  If you really want to dive into what this system is all about and explore it then " +
            "select \"(L)et's go to the BBS!\" and you'll be plopped into the main chat room, from there you can go to the text files " +
            "area by typing '/text'.";

        private static readonly string _mutiny =
            "Okay so here's the deal.  Are there games here?  Well potentially, you see this system has a built-in online Basic programming environment which allows users " +
            "such as yourself to write your own games and other programs and let other users run them.  But before you can get to exploring those you first need to " +
            "move beyond this faux-main-menu by pressing \"(L)et's go to the BBS!\" and then you'll be plopped into the main area which is a chat room with history " +
            "and from there you can use the /files command to access the file system and start looking for those basic programs.  "+
            "If you *really* want to play door games or download files then why not check out Mutiny BBS at this address " +
            "on port 2332 (mutinybbs.com:2332) or if you prefer SSH over Telnet then use port 2232.";

        /// <summary>
        /// Returns true if the user has decided to keep using the BBS in normal (chat) mode, otherwise false if they want to leave
        /// </summary>
        public static bool Execute(BbsSession session)
        {
            Action Pause = () =>
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    session.Io.Output("Press any key to continue...");
                    session.Io.InputKey();
                    session.Io.OutputLine();
                }
            };

            var originalLocation = session.CurrentLocation;
            var originalDnd = session.DoNotDisturb;
            session.DoNotDisturb = true;
            session.CurrentLocation = Core.Enums.Module.FauxMain;

            try
            {
                do
                {
                    session.Io.OutputLine(string.Join(Environment.NewLine, _menu));
                    session.Io.Output("'Main Menu' > ");
                    var key = session.Io.InputKey();
                    session.Io.OutputLine();
                    if (key.HasValue)
                    {
                        switch (char.ToUpper(key.Value))
                        {
                            case 'M':
                            case 'C':
                                session.Io.OutputLine(_messageboards);
                                Pause();
                                break;
                            case 'T':
                                session.Io.OutputLine(_textfiles);
                                Pause();
                                break;
                            case 'G':
                            case 'D':
                                session.Io.OutputLine(_mutiny);
                                Pause();
                                break;
                            case 'O':
                                return false;
                            case 'L':
                                return true;
                        }
                    }
                } while (true);
            } finally
            {
                session.DoNotDisturb = originalDnd;
                session.CurrentLocation = originalLocation;
            }
        }
    }
}
