using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using miniBBS.Services;
using miniBBS.Services.GlobalCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class Msg
    {
        private static readonly ChatWriteFlags _chatWriteFlags = 
            ChatWriteFlags.UpdateLastReadMessage | 
            ChatWriteFlags.UpdateLastMessagePointer | 
            ChatWriteFlags.FormatForMessageBase;

        public static void Execute(BbsSession session)
        {
            var previousDnd = session.DoNotDisturb;
            var previousLoc = session.CurrentLocation;
            var prevPrompt = session.ShowPrompt;
            session.ShowPrompt = () => Prompt(session);

            try
            {
                SetMessagePointer.SetToFirstUnreadMessage(session);
                var chat = ShowNextMessage.Execute(session, _chatWriteFlags);
                FindThreadStart(session, ref chat);
                int threadStart = chat.Id;

                var threadQ = new Queue<Chat>();

                Action NextThread = () =>
                {
                    var newChat = session.Chats.Values.FirstOrDefault(x => x.Id > threadStart && !x.ResponseToId.HasValue);
                    if (newChat != null)
                    {
                        chat = newChat;
                        threadStart = newChat.Id;
                        WriteMessage(session, ref chat);
                    }
                    else
                        session.Io.Error("No more threads, use ] to advance to next channel.");
                };

                Action PrevThread = () =>
                {
                    var newChat = session.Chats.Values.LastOrDefault(x => x.Id < threadStart && !x.ResponseToId.HasValue);
                    if (newChat != null)
                    {
                        chat = newChat;
                        threadStart = newChat.Id;
                        WriteMessage(session, ref chat);
                    }
                    else
                        session.Io.Error("No earlier threads, use ] to advance to next channel.");
                };

                do
                {
                    session.CurrentLocation = Module.MessageBase;
                    session.DoNotDisturb = true;
                    session.ShowPrompt();
                    var c = session.Io.InputKey();

                    if (c != null && c != '/' && !char.IsDigit(c.Value))
                        session.Io.OutputLine();

                    switch (c)
                    {
                        case '?':
                            session.Io.OutputLine(_menu);
                            break;
                        case '/':
                            session.Io.Output(c.Value);
                            HandleSlashCommand(session, GetSlashCommand(session));
                            break;
                        case ']':
                        case '}':
                            {
                                var chans = new SortedList<int, Channel>(GetChannel.GetChannels(session)
                                    .ToDictionary(k => k.Id));
                                var currentChannelNumber = chans.ItemNumber(session.Channel.Id);
                                int nextChannelNumber = currentChannelNumber.Value + 1;
                                if (nextChannelNumber >= chans.Count)
                                    nextChannelNumber = 0;
                                SwitchOrMakeChannel.Execute(session, $"{nextChannelNumber + 1}", false, fromMessageBase: true);
                                chat = ShowNextMessage.Execute(session, _chatWriteFlags);
                                threadQ.Clear();
                            }
                            break;
                        case '[':
                        case '{':
                            {
                                var chans = new SortedList<int, Channel>(GetChannel.GetChannels(session)
                                    .ToDictionary(k => k.Id));
                                var currentChannelNumber = chans.ItemNumber(session.Channel.Id);
                                int nextChannelNumber = currentChannelNumber.Value - 1;
                                if (nextChannelNumber < 0)
                                    nextChannelNumber = chans.Count - 1;
                                SwitchOrMakeChannel.Execute(session, $"{nextChannelNumber + 1}", false, fromMessageBase: true);
                                chat = ShowNextMessage.Execute(session, _chatWriteFlags);
                                threadQ.Clear();
                            }
                            break;
                        case (char)13:
                        case '>':
                        case '.':
                            chat = GetNextChatInThread(session, chat, threadQ);
                            if (chat != null)
                                WriteMessage(session, ref chat);
                            else
                            {
                                session.Io.Error("You have reached the end of this thread, going (F)orward to the next...");
                                NextThread();
                            }
                            break;
                        case '<':
                        case ',':
                            threadQ.Clear();
                            if (chat?.ResponseToId != null && session.Chats.ContainsKey(chat.ResponseToId.Value))
                            {
                                SetMessagePointer.Execute(session, chat.ResponseToId.Value, reverse: true);
                                chat = session.Chats[chat.ResponseToId.Value];
                                WriteMessage(session, ref chat);
                            }
                            else
                            {
                                session.Io.Error("You have reached the start of this thread, going (B)ack to the previous...");
                                PrevThread();
                            }
                            break;
                        case 'f':
                        case 'F':
                            NextThread();
                            break;
                        case 'b':
                        case 'B':
                            PrevThread();
                            break;
                        case 'p':
                        case 'P':
                        case 'r':
                        case 'R':
                            {
                                PostChatFlags flags = char.ToLower(c.Value) == 'r' ? PostChatFlags.None : PostChatFlags.IsNewTopic;
                                PostMessage(session, flags);
                            }
                            break;
                        case 'l':
                        case 'L':
                            ListMessages(session, chat.Id);
                            break;
                        case 'c':
                        case 'C':
                            ListChannels.Execute(session);
                            break;
                        case 'q':
                        case 'Q':
                            session.Io.Error("Leaving message base mode.");
                            return;
                        default:
                            if (c.HasValue && char.IsDigit(c.Value))
                            {
                                int msgNum = InputMessageNumber(session, c.Value);
                                var msgId = session.Chats.ItemKey(msgNum);
                                if (msgId.HasValue && session.Chats.ContainsKey(msgId.Value))
                                {
                                    chat = session.Chats[msgId.Value];
                                    WriteMessage(session, ref chat);
                                    Chat tmp = chat;
                                    FindThreadStart(session, ref tmp);
                                    threadStart = tmp.Id;
                                }
                                else
                                    session.Io.Error($"Unrecognized message number {msgNum}");
                            }
                            break;
                    }
                } while (!session.ForceLogout && session.Stream.CanRead && session.Stream.CanWrite);
            }
            finally
            {
                session.DoNotDisturb = previousDnd;
                session.CurrentLocation = previousLoc;
                session.ShowPrompt = prevPrompt;
            }
        }

        private static Chat GetNextChatInThread(BbsSession session, Chat prevChat, Queue<Chat> threadQ)
        {
            Chat result = null;

            if (threadQ.Count > 0)
                result = threadQ.Dequeue();
            else if (prevChat != null)
                result = session.Chats?.Values.Where(x => x.Id > prevChat.Id && x.ResponseToId == prevChat.Id)?.FirstOrDefault();

            if (result != null)
            {
                var responses = session.Chats?.Values?.Where(x => x?.Id > result?.Id && x?.ResponseToId == result?.Id)?.ToList();
                if (true == responses?.Any())
                {
                    foreach (var response in responses)
                        threadQ.Enqueue(response);
                }
            }

            return result;
        }

        private static void FindThreadStart(BbsSession session, ref Chat chat)
        {
            while (chat.ResponseToId.HasValue && session.Chats.ContainsKey(chat.ResponseToId.Value))
                chat = session.Chats[chat.ResponseToId.Value];
        }

        private static int InputMessageNumber(BbsSession session, char startingDigit)
        {
            var list = new List<char>();
            list.Add(startingDigit);
            session.Io.Output(startingDigit);
            do
            {
                var c = session.Io.InputKey();
                if (!c.HasValue || !char.IsDigit(c.Value))
                    break;
                list.Add(c.Value);
                session.Io.Output(c.Value);
            } while (true);
            session.Io.OutputLine();
            var str = new string(list.ToArray());
            return int.Parse(str);
        }

        private static bool _startAtCurrent = true;
        private static bool _showUnreadOnly = true;
        private static bool _showThreadStartsOnly = true;

        private static void ListMessages(BbsSession session, int startingId)
        {
            if (!SetListOptions(session, startingId))
                return;

            var builder = new StringBuilder();
            builder.AppendLine("--- Listing Messages ---");

            IEnumerable<KeyValuePair<int, Chat>> chats = session.Chats;
            if (_startAtCurrent)
                chats = chats.Where(c => c.Key >= startingId);
            if (_showUnreadOnly)
            {
                var readIds = session.ReadChatIds(DI.Get<IDependencyResolver>());
                chats = chats.Where(c => !readIds.Contains(c.Key));
            }
            if (_showThreadStartsOnly)
                chats = chats.Where(c => c.Value.ResponseToId == null);

            foreach (var c in chats)
                builder.AppendLine(IndexBy.FormatLine(session, c.Value));

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.Output(builder.ToString());
            }

            session.Io.OutputLine("To change to a specific message, just type the message number.".Color(ConsoleColor.White));
        }

        private static bool SetListOptions(BbsSession session, int currentMessageId)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                bool exitMenu = false;
                var currentMessageNum = session.Chats.ItemNumber(currentMessageId);
                while (!exitMenu)
                {
                    session.Io.OutputLine("** List Messages Options **");
                    session.Io.OutputLine("A) Start at : " + $"{(_startAtCurrent ? $"Current Message (>= {currentMessageNum})" : "First Message (>= 0)")}".Color(ConsoleColor.Yellow));
                    session.Io.OutputLine("B) Show unread messages only : " + $"{_showUnreadOnly}".Color(ConsoleColor.Yellow));
                    session.Io.OutputLine("C) Show only starts of threads : " + $"{_showThreadStartsOnly}".Color(ConsoleColor.Yellow));
                    session.Io.OutputLine("ENTER) List Messages".Color(ConsoleColor.Green));
                    session.Io.OutputLine("Q) Quit");
                    var k = session.Io.Ask("List Messages");
                    switch (k)
                    {
                        case 'A': _startAtCurrent = !_startAtCurrent; break;
                        case 'B': _showUnreadOnly = !_showUnreadOnly; break;
                        case 'C': _showThreadStartsOnly = !_showThreadStartsOnly; break;
                        case 'Q': return false;
                        default: exitMenu = true; break;
                    }
                }
            }
            return true;
        }

        public static void PostMessage(BbsSession session, PostChatFlags flags)
        {
            var editor = DI.Get<ITextEditor>();
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                if (flags.HasFlag(PostChatFlags.IsNewTopic))
                    session.Io.OutputLine($"{Constants.Spaceholder} --- Posting a new message in {session.Channel.Name} ---");                
                else
                    session.Io.OutputLine($"{Constants.Spaceholder} --- Posting response (original message will be quoted automatically) ---");
            }

            editor.OnSave = body =>
            {
                AddToChatLog.Execute(session, body, flags);
                return string.Empty;
            };

            editor.EditText(session, new LineEditorParameters
            {
                QuitOnSave = true
            });
        }

        private static void HandleSlashCommand(BbsSession session, string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            if (command.StartsWith("/ch", StringComparison.CurrentCultureIgnoreCase) &&
                command.Length > 3 &&
                int.TryParse(command.Substring(3).Trim(), out var chanNum))
            {
                SwitchOrMakeChannel.Execute(session, chanNum.ToString(), false, true);
                return;
            }

            switch (command.ToLower())
            {
                case "/textread":
                case "/tr":
                case "/run":
                case "/exec":
                    ReadTextFile.Execute(session);
                    break;
                default:
                    session.Io.Error("Unrecognized slash command from message base mode, press Q to quit back to chat mode.");
                    break;
            }
        }

        private static string GetSlashCommand(BbsSession session)
        {
            var line = session.Io.InputLine();
            return "/" + line;
        }

        private static void WriteMessage(BbsSession session, ref Chat chat)
        {
            chat.Write(session, _chatWriteFlags, GlobalDependencyResolver.Default);
        }

        private static readonly Action<BbsSession> Prompt = (session) =>
        {
            session.Io.SetForeground(ConsoleColor.Cyan);

            var lastRead = session.Chats.ItemNumber(session.LastMsgPointer) ?? -1;
            var count = session.Chats?.Count - 1 ?? 0;

            var prompt =
                $"[Message Base: {session.Channel.Name}] (?=Help) > ".Color(ConsoleColor.White);

            session.Io.Output(prompt);
            session.Io.SetForeground(ConsoleColor.White);
        };

        private static readonly string _menu =
            $" *** {Constants.BbsName} Message Base Menu *** \n".Color(ConsoleColor.Magenta) +
            Environment.NewLine +
            "[".Color(ConsoleColor.Green) + "=".Color(ConsoleColor.DarkGray) + "Previous Channel  ".Replace(" ", $"{Constants.Spaceholder}").Color(ConsoleColor.White) +
            "]".Color(ConsoleColor.Green) + "=".Color(ConsoleColor.DarkGray) + "Next Channel".Color(ConsoleColor.White) +
            Environment.NewLine +
            "<".Color(ConsoleColor.Green) + "=".Color(ConsoleColor.DarkGray) + "Previous Message  ".Replace(" ", $"{Constants.Spaceholder}").Color(ConsoleColor.White) +
            ">".Color(ConsoleColor.Green) + "/".Color(ConsoleColor.DarkGray) + "ENTER".Color(ConsoleColor.Green) + "=".Color(ConsoleColor.DarkGray) + "Next Msg".Color(ConsoleColor.White) +
            Environment.NewLine + 
            "B".Color(ConsoleColor.Green) + "=".Color(ConsoleColor.DarkGray) + "Back One Thread   ".Replace(" ", $"{Constants.Spaceholder}").Color(ConsoleColor.White) +
            "F".Color(ConsoleColor.Green) + "=".Color(ConsoleColor.DarkGray) + "Fwd. One Thread".Color(ConsoleColor.White) +
            Environment.NewLine +
            "R".Color(ConsoleColor.Green) + "=".Color(ConsoleColor.DarkGray) + "Reply to Message  ".Replace(" ", $"{Constants.Spaceholder}").Color(ConsoleColor.White) +
            "P".Color(ConsoleColor.Green) + "=".Color(ConsoleColor.DarkGray) + "Post New".Color(ConsoleColor.White) +
            Environment.NewLine +
            "L".Color(ConsoleColor.Green) + "=".Color(ConsoleColor.DarkGray) + "List Messages     ".Replace(" ", $"{Constants.Spaceholder}").Color(ConsoleColor.White) +
            "C".Color(ConsoleColor.Green) + "=".Color(ConsoleColor.DarkGray) + "List Channels".Color(ConsoleColor.White) +
            Environment.NewLine + 
            "Q".Color(ConsoleColor.Green) + "=".Color(ConsoleColor.DarkGray) + "Quit Msg Bases".Color(ConsoleColor.White);

    }
}
