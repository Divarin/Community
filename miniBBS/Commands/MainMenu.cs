using miniBBS.Core;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using miniBBS.Services.GlobalCommands;
using System;

namespace miniBBS.Commands
{
    public static class MainMenu
    {
        private static readonly Func<string, ConsoleColor, string> _clr = (txt, clr) => UserIoExtensions.WrapInColor(txt, clr);

        private static readonly string[] _menu = new[]
        {
            $"{_clr("***", ConsoleColor.Yellow)} {_clr($"{Constants.Inverser}Mutiny Community Main Menu{Constants.Inverser}", ConsoleColor.Magenta)} {_clr("***", ConsoleColor.Yellow)} ",
            $"{_clr("M", ConsoleColor.Green)}) Message Bases / Bulletin Boards",
            $"{_clr("C", ConsoleColor.Green)}) Chat Rooms (with history)",
            $"{_clr("L", ConsoleColor.Green)}) Live Chat Calendar",
            $"{_clr("E", ConsoleColor.Green)}) E-Mail",
            $"{_clr("T", ConsoleColor.Green)}) Text Files",
            $"{_clr("V", ConsoleColor.Green)}) Voting Booth",
            $"{_clr("G", ConsoleColor.Green)}) Games",
            $"{_clr("W", ConsoleColor.Green)}) Who is on",
            $"{_clr("U", ConsoleColor.Green)}) User List",
            $"{_clr("O", ConsoleColor.Green)}) Logoff",
            //$"{_clr("L", ConsoleColor.Green)}) Learn how to do more with Community!"
        };

//        private const string _learn =
//"Mutiny Community BBS is not a traditional BBS.  The vast majority of its functionality is based in the Chat Rooms because " +
//"this is primarily a chat based system.  The chats posted to the chat rooms are persisted and act as a message base.  That means you can read " +
//"and respond to chats that have been left in the past and you can post chats that can be read in the future. " +
//"\r\n\r\n" +
//"This means that the M)essage Base messages and the C)hat room messages are one and the same.  The only difference is the look and feel of how you " +
//"interact with those messages.  If you prefer a traditional Message Base style then feel free to use it.  However, in this mode, you cannot engage in real-time chats " +
//"with users who are currently on Community.  Alternatively you can use the Chat room interface to read and post messages and if people are online " +
//"right now they will see these messages and can respond right away and you'll see their response and thus real-time conversation will ensue!" +
//"\r\n\r\n" +
//"Since this BBS was originally built out as a chat based system most of the functionality is accessed via slash commands (commands starting with a '/') " +
//"in the chat room.  For example you can /whisper to another user or /wave to users or run bots using commands like /guessbot. " +
//"\r\n\r\n" +
//"You can also create your own channels (remember a chat channel *IS* a message base, they are one and the same).  But you can only create channels from chat mode. " +
//"Although, once created they are accessible from the Message Base interface as well." +
//"\r\n\r\n" +
//"Everything that you can do from this Main Menu can be done through some sort of slash command in chat and more.  There is a lot of functionality that is not " +
//"covered in this main menu.  The main menu is to help new users who are familiar with traditional BBSs.  If you want to learn more about the unique features " +
//"of Community then go into C)hat mode and use the slash command '/?' to start learning.";

        /// <summary>
        /// Returns true if the user has decided to keep using the BBS in normal (chat) mode, otherwise false if they want to leave
        /// </summary>
        public static bool Execute(BbsSession session)
        {
            void Pause()
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    session.Io.Output("Press any key to continue...");
                    session.Io.InputKey();
                    session.Io.OutputLine();
                }
            }

            var originalLocation = session.CurrentLocation;
            var originalDnd = session.DoNotDisturb;
            session.DoNotDisturb = true;
            session.CurrentLocation = Core.Enums.Module.MainMenu;

            try
            {
                while (!session.ForceLogout && session.Stream.CanRead && session.Stream.CanWrite)
                {
                    session.Io.OutputLine(string.Join(Environment.NewLine, _menu));
                    session.Io.Output($"{Constants.Inverser}[Main Menu] >{Constants.Inverser} ");
                    var key = session.Io.InputKey();
                    session.Io.OutputLine();
                    if (key.HasValue)
                    {
                        switch (char.ToUpper(key.Value))
                        {
                            case 'M':
                            case 'B':
                                //EnterMessageBaseOrBulletins(session);
                                Bulletins.Execute(session);
                                break;
                            case 'C':
                                //Tutor.Execute(session, "If you prefer a more traditional message base format type '/msg', you'll be reading the same messages either way.");
                                session.Io.Error("Use '/main' to return to main menu.");
                                return true;
                            case 'E':
                                Mail.Execute(session);
                                break;
                            case 'T':
                                {
                                    var browser = DI.Get<ITextFilesBrowser>();
                                    browser.OnChat = line =>
                                    {
                                        AddToChatLog.Execute(session, line);
                                    };
                                    browser.Browse(session);
                                }
                                break;
                            case 'V':
                                Polls.Execute(session);
                                break;
                            case 'G':
                                BrowseGames.Execute(session);
                                break;
                            case 'O':
                                if ('Y' == session.Io.Ask("Are you sure you want to log off?"))
                                    return false;
                                break;
                            case 'L':
                                //session.Io.OutputLine(_learn);
                                //Pause();
                                Calendar.Execute(session);
                                break;
                            case 'W':
                                WhoIsOn.Execute(session);
                                Pause();
                                break;
                            case 'U':
                                WhoIsAll.Execute(session);
                                Pause();
                                break;
                        }
                    }
                };

                return false;
            }
            finally
            {
                session.DoNotDisturb = originalDnd;
                session.CurrentLocation = originalLocation;
            }
        }

        private static void EnterMessageBaseOrBulletins(BbsSession session)
        {
            var paren = ")".Color(ConsoleColor.DarkGray);

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Cyan))
            {
                session.Io.OutputLine("Choose:");
                session.Io.OutputLine("B".Color(ConsoleColor.Green) + paren + " Bulletin Board" +
                    " - A traditional BBS message board, separate from the online/offline chat rooms.".Color(ConsoleColor.DarkGray));
                session.Io.OutputLine("M".Color(ConsoleColor.Green) + paren + " Message Base format for Chat" +
                    " - A message base formatted interface for the online/offline chat rooms.".Color(ConsoleColor.DarkGray));
                session.Io.OutputLine("Q".Color(ConsoleColor.Green) + paren + " Quit" +
                    " - Back to main menu.".Color(ConsoleColor.DarkGray));

                var key = session.Io.Ask("Choose");
                switch (key)
                {
                    case 'B':
                        Bulletins.Execute(session);
                        break;
                    case 'M':
                        Tutor.Execute(session, "The Message base and the Chat rooms contain the same messages.  The Message base formats these into a message base style but because most of the messages were entered using that chat rooms, and about half of those were during real-time chats you might find the chat room interface better.");
                        Msg.Execute(session);
                        break;
                    default:
                        return;
                }
                
            }
        }
    }
}
