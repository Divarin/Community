using miniBBS.Core;
using miniBBS.Core.Enums;
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
            $"{_clr("***", ConsoleColor.Yellow)} {_clr($"{Constants.Inverser}{Constants.BbsName} Main Menu{Constants.Inverser}", ConsoleColor.Magenta)} {_clr("***", ConsoleColor.Yellow)} ",
            $"{_clr($"{Constants.Inverser}M{Constants.Inverser}", ConsoleColor.Green)}) Message Bases / Bulletin Boards",
            $"{_clr($"{Constants.Inverser}C{Constants.Inverser}", ConsoleColor.Green)}) Chat Rooms (with history)",
            $"{_clr($"{Constants.Inverser}N{Constants.Inverser}", ConsoleColor.Green)}) NullSpace Chat",
            $"{_clr($"{Constants.Inverser}L{Constants.Inverser}", ConsoleColor.Green)}) Live Chat Calendar",
            $"{_clr($"{Constants.Inverser}E{Constants.Inverser}", ConsoleColor.Green)}) E-Mail",
            $"{_clr($"{Constants.Inverser}F{Constants.Inverser}", ConsoleColor.Green)}) File System",
            $"{_clr($"{Constants.Inverser}G{Constants.Inverser}", ConsoleColor.Green)}) Gopher",
            $"{_clr($"{Constants.Inverser}V{Constants.Inverser}", ConsoleColor.Green)}) Voting Booth",
            $"{_clr($"{Constants.Inverser}D{Constants.Inverser}", ConsoleColor.Green)}) Door Games",
            $"{_clr($"{Constants.Inverser}W{Constants.Inverser}", ConsoleColor.Green)}) Who is on",
            $"{_clr($"{Constants.Inverser}U{Constants.Inverser}", ConsoleColor.Green)}) User List",
            $"{_clr($"{Constants.Inverser}P{Constants.Inverser}", ConsoleColor.Green)}) Preferences",
            $"{_clr($"{Constants.Inverser}O{Constants.Inverser}", ConsoleColor.Green)}) Logoff",
        };

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
            session.CurrentLocation = Module.MainMenu;
            session.DoNotDisturb = true;

            try
            {
                while (!session.ForceLogout && session.Stream.CanRead && session.Stream.CanWrite)
                {
                    session.Io.SetForeground(ConsoleColor.White);

                    if (!DI.Get<IMenuFileLoader>().TryShow(session, MenuFileType.Main))
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
                                originalLocation = Module.Chat;
                                return true;
                            case 'N':
                                NullSpace.Enter(session);
                                break;
                            case 'E':
                                Mail.Execute(session);
                                break;
                            case 'G':
                                Gopher.Execute(session);
                                break;
                            case 'F':
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
                            case 'D':
                                BrowseGames.Execute(session);
                                break;
                            case 'O':
                                {
                                    var k = session.Io.Ask("Logoff? (Y)es, (W)ith Message, (N)o");
                                    if (k == 'W')
                                    {
                                        session.Io.Output("Enter goodbye message: ");
                                        var quitMessage = session.Io.InputLine();
                                        session.Io.OutputLine();
                                        if (!string.IsNullOrWhiteSpace(quitMessage))
                                            session.Items[SessionItem.LogoutMessage] = quitMessage;
                                    }
                                    if (k == 'W' || k == 'Y')
                                    {
                                        session.Items[SessionItem.DoNotShowDndSummary] = true;
                                        return false;
                                    }
                                }
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
                            case 'P':
                                UserPreferences.Execute(session);
                                break;
                        }
                    }
                };

                return false;
            }
            finally
            {
                session.CurrentLocation = originalLocation;
                session.DoNotDisturb = originalDnd;
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
