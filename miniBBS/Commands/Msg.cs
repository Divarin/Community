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

            Tutor.Execute(session, _warning);

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
                    session.DoNotDisturb = true;
                    session.CurrentLocation = Module.MessageBase;                    
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
                                int? nextChannelNumber = currentChannelNumber.Value + 1;
                                var nextChannelId = chans.ItemKey(nextChannelNumber.Value) ?? chans.First().Key;
                                nextChannelNumber = chans.ItemNumber(nextChannelId);
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
                                int? nextChannelNumber = currentChannelNumber.Value - 1;
                                var nextChannelId = chans.ItemKey(nextChannelNumber.Value) ?? chans.Last().Key;
                                nextChannelNumber = chans.ItemNumber(nextChannelId);
                                SwitchOrMakeChannel.Execute(session, $"{nextChannelNumber + 1}", false, fromMessageBase: true);
                                chat = ShowNextMessage.Execute(session, _chatWriteFlags);
                                threadQ.Clear();
                            }
                            break;
                        case (char)13:
                        case '>':
                        case '.':
                            if (threadQ.Count > 0)
                            {
                                chat = threadQ.Dequeue();
                                WriteMessage(session, ref chat);
                            }
                            else
                            {
                                var newChats = session.Chats.Values.Where(x => x.Id > chat.Id && x.ResponseToId == chat.Id).ToList();
                                if (newChats.Count == 0)
                                {
                                    session.Io.Error("You have reached the end of this thread, going (F)orward to the next...");
                                    NextThread();
                                }
                                else
                                {
                                    chat = newChats.First();
                                    WriteMessage(session, ref chat);
                                    for (int i = 1; i < newChats.Count; i++)
                                        threadQ.Enqueue(newChats[i]);
                                }
                            }
                            break;
                        case '<':
                        case ',':
                            threadQ.Clear();
                            if (chat.ResponseToId.HasValue && session.Chats.ContainsKey(chat.ResponseToId.Value))
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
                            //{
                            //    int loops = chat.ResponseToId.HasValue ? 2 : 1;
                            //    var newChat = chat;
                            //    for (int i = 0; i < loops; i++)
                            //    {
                            //        newChat = session.Chats.Values.LastOrDefault(x => x.Id < newChat.Id && !x.ResponseToId.HasValue);
                            //    }
                            //    if (newChat != null)
                            //    {
                            //        chat = newChat;
                            //        WriteMessage(session, ref chat);
                            //    }
                            //    else
                            //        session.Io.Error("No earlier threads, use ] to advance to next channel.");
                            //}
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

        private static void ListMessages(BbsSession session, int startingId)
        {
            var builder = new StringBuilder();
            builder.AppendLine("--- Listing start of new threads ---");
            builder.AppendLine("(Unread message threads only)".Color(ConsoleColor.DarkGray));

            var readIds = session.ReadChatIds(DI.Get<IDependencyResolver>());

            foreach (var c in session.Chats.Where(c => !readIds.Contains(c.Key) && c.Value.ResponseToId == null))
                builder.AppendLine(IndexBy.FormatLine(session, c.Value));

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.Output(builder.ToString());
            }

            session.Io.OutputLine("To change to a specific message, just type the message number.".Color(ConsoleColor.White));
        }

        private static void PostMessage(BbsSession session, PostChatFlags flags)
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
            " *** Community Message Base Menu *** \n".Color(ConsoleColor.Magenta) +
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

        private const string _warning =
            "The 'Message Base' is an alternate view of the Chat Rooms.  These messages are actually chat room messages.  Threading is accomplished by use of 're:' numbers (see help on 'content' for more info).  " +
            "This view is not perfect it's a best attempt at formatting chat room messages in a traditional BBS message base style.  You are free to post and respond to old messages as you can in chat " +
            "(If you want to respond to Jimbob's post, Jimbob doesn't have to be line in order to see it, he'll see it when he comes back, even if he's using the chat view) " +
            "\r\n\r\n" +
            "This 'Message Base' view is not perfect and there are sometimes breaks in threads when a chat message gets deleted.  Also some of the earlier messages were added before " +
            "threading was possible so they show up as separate threads." +
            "\r\n\r\n" +
            "In other words, to get the most out of Mutiny Community, it is encouraged that you use that Chat mode instead of the Message Base mode.  Still you're welcome to use the Message Base format but consider yourself " +
            "warned that you may be missing out on some of the messages.";
    }
}
