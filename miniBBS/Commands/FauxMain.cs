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
            $"{_clr("W", ConsoleColor.Green)}) What's up with this faux menu?",
            $" {_clr("---", ConsoleColor.DarkGray)} ",
            $"{_clr("L", ConsoleColor.Green)}) Let's go to the BBS!"
        };

        private const string _messageboards =
            "Messages and chats are the same thing.  Want to start reading and posting? \r\n" +
            "Press L for \"Let's go to the BBS\", then just keep pressing enter to read messages. \r\n" +
            "To respond or post just type what's on your mind!";

        //"Okay so here's the deal.  This isn't a 'normal' BBS and although there certainly are messages here " +
        //"things aren't going to act and feel like you're expecting.  In-fact this whole 'main menu' is a sham. " +
        //$"{Environment.NewLine}" +
        //$"There is no main menu, really.  So you want to read the messages?  Here's how you do it.  {Environment.NewLine}" +
        //"First,\"(L)et's go to the BBS!\" to get out of this faux-main-menu.  From there you'll end up in a chat room that will " +
        //"look a lot like IRC, or MRC, or any other real-time chat system you've ever used.  Except here's the thing, the chat rooms *are* the " +
        //"message boards because although people can chat back and forth in real time you can also read all of the chats left before you logged " +
        //$"in which means that it acts like a message board as well. {Environment.NewLine}" +
        //$"{Environment.NewLine}" +
        //"So chat rooms are really the 'main menu' and everywhere you can go on this BBS originates there.  All commands start with a slash " +
        //$"so for example '/files' will take you to the text files subsection and '/mail' will take you to the email subsection. {Environment.NewLine}" +
        //$"{Environment.NewLine}" +
        //"To read the messages all you have to do is keep pressing enter to advance to the next message.  If you want to respond just type a response and " +
        //"will automatically be flagged as being 'in-response-to' whatever message you just read.  It may seem a little strange responding to a 'chat message' " +
        //"left months or years ago but don't worry that's how things roll here on Community conversations drift back and forth between real-time and not." +
        //$"{Environment.NewLine}{Environment.NewLine}" +
        //"Be sure to check out the '/?' command to see what all commands you can use and what they do.";

        private const string _textfiles =
            "Want to read (or write) some text files? \r\n" +
            "Press L for \"Let's go to the BBS\", then type /files. \r\n" +
            "Navigate the filesystem like you would DOS/*nix, use command \"HELP\" if you get lost.";

        //"Okay so here's the deal.  There is a pretty extensive text files subsection on this BBS which includes a mirror of the Jason Scott " +
        //"textfiles.com archive as well as the ability for users such as yourself to write up your own text files and even allows for " +
        //"multiple users to collaborate together on writing projects.  But, in order to get to this you must first press L for " +
        //"\"(L)et's go to the BBS!\" and move beyond this faux-main-menu.  You see this isn't really a main menu, in-fact this BBS doesn't have a main menu. " +
        //"It's a chat system with history meaning that the chat room doubles as the message base.  So really you're supposed to 'land' in the " +
        //"chat rooms when you log in.  This so-called 'main menu' is just being shown because you're new here and people *expect* BBSs to work " +
        //"in a certain way and get flustered when they don't.  If you really want to dive into what this system is all about and explore it then " +
        //"select \"(L)et's go to the BBS!\" and you'll be plopped into the main chat room, from there you can go to the text files " +
        //"area by typing '/text'.";

        private const string _games =
            "Want to play (or make) some games? \r\n " +
            "Press L for \"Let's go to the BBS\", then type /files. \r\n " +
            "Explore the CommunityUsers area using DOS/*nix like commands, and look for BASIC (.bas) files. \r\n" +
            "If you're feeling nostalgic for more familiar BBS door games check out mutinybbs.com:2332";

        //"Okay so here's the deal.  Are there games here?  Well potentially, you see this system has a built-in online Basic programming environment which allows users " +
        //"such as yourself to write your own games and other programs and let other users run them.  But before you can get to exploring those you first need to " +
        //"move beyond this faux-main-menu by pressing \"(L)et's go to the BBS!\" and then you'll be plopped into the main area which is a chat room with history " +
        //"and from there you can use the /files command to access the file system and start looking for those basic programs.  " +
        //"If you *really* want to play door games or download files then why not check out Mutiny BBS at this address " +
        //"on port 2332 (mutinybbs.com:2332) or if you prefer SSH over Telnet then use port 2232.";

        private const string _downloads =
            "File downloads not yet implemented, I'm working on it.";

        private const string _fauxmain =
            "Community is a very unique BBS and works differently to what users usually expect. \r\n" +
            "A lot of users will, after login, just keep pressing enter until they get to something that looks like a menu. \r\n" +
            "As you get familiar with this system you'll see why that won't work here but for now since you're new here " +
            "I've brought you to this faux (fake) 'main menu' to try to ease you into the world of Community. \r\n" +
            "You'll only see this the first time you log in but you can always come back to it by using the command /main.";

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
                                session.Io.OutputLine(_games);
                                Pause();
                                break;
                            case 'D':
                                session.Io.OutputLine(_downloads);
                                Pause();
                                break;
                            case 'O':
                                if ('Y' == session.Io.Ask("Are you sure you want to log off?  You haven't even really been to the BBS yet!"))
                                    return false;
                                break;
                            case 'W':
                                session.Io.OutputLine(_fauxmain);
                                Pause();
                                break;
                            case 'L':
                                return true;
                        }
                    }
                } while (true);
            }
            finally
            {
                session.DoNotDisturb = originalDnd;
                session.CurrentLocation = originalLocation;
            }
        }
    }
}
