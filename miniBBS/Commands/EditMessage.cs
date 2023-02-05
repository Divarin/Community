using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using miniBBS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class EditMessage
    {
        public static void Execute(BbsSession session, string line, bool useLineEditor = false)
        {
            string[] args = SplitArguments(line).ToArray();

            if (useLineEditor && true == args?.Length > 1)
                useLineEditor = false;

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
            {
                if (!useLineEditor && args?.Length < 2)
                {
                    ShowUsage(session);
                    return;
                }

                Chat toBeEdited = FindChatToBeEdited(session, args);

                if (toBeEdited == null)
                    session.Io.OutputLine("Messagee not found in this channel.");

                bool canUpdate =
                    session.User.Access.HasFlag(AccessFlag.Administrator) ||
                    session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                    session.UcFlag.Flags.HasFlag(UCFlag.Moderator) ||
                    (
                        toBeEdited.FromUserId == session.User.Id &&
                        (DateTime.UtcNow - toBeEdited.DateUtc).TotalMinutes <= Constants.MinutesUntilMessageIsUndeletable
                    );

                if (!canUpdate)
                {
                    session.Io.OutputLine($"Cannot edit message.  It's either too old (more than {Constants.MinutesUntilMessageIsUndeletable} minutes) or you aren't a moderator.");
                    return;
                }

                void commitEdit(string _text)
                {
                    string message = string.Join(Environment.NewLine, new[]
{
                        $"{session.User.Name} edited message # {session.Chats.ItemNumber(toBeEdited.Id)} in channel {session.Channel.Name}",
                        "The message now reads:",
                        _text
                    }); ;

                    toBeEdited.Message = _text;
                    session.Io.OutputLine("Done.");
                    DI.GetRepository<Chat>().Update(toBeEdited);
                    session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, message)
                    {
                        OnReceive = (s) => s.Chats[toBeEdited.Id].Message = _text
                    });
                }

                string newMessage = string.Empty;
                if (useLineEditor)
                {
                    var editor = DI.Get<ITextEditor>();
                    editor.OnSave = _body =>
                    {
                        commitEdit(_body);
                        return string.Empty;
                    };
                    editor.EditText(session, new LineEditorParameters
                    {
                        PreloadedBody = toBeEdited.Message,
                        QuitOnSave = true
                    });
                }
                else
                {
                    int searchArgIndex = args.Length >= 3 ? 1 : 0;
                    int replaceArgIndex = searchArgIndex + 1;
                    string search = args.Length > searchArgIndex ? args[searchArgIndex] : null;
                    string replace = args.Length > replaceArgIndex ? args[replaceArgIndex] : null;

                    if (string.IsNullOrWhiteSpace(search) || string.IsNullOrWhiteSpace(replace))
                    {
                        ShowUsage(session);
                        return;
                    }

                    string oldMessage = toBeEdited.Message;
                    newMessage = toBeEdited.Message.Replace(search, replace);
                    session.Io.SetForeground(ConsoleColor.White);
                    string highlightedNewMessage = oldMessage.Replace(search, UserIoExtensions.WrapInColor(replace, ConsoleColor.Green));
                    session.Io.OutputLine(highlightedNewMessage);
                    session.Io.SetForeground(ConsoleColor.Red);
                    session.Io.Output("Make this edit? ");
                    var k = session.Io.InputKey();
                    session.Io.OutputLine();
                    if (k == 'y' || k == 'Y')
                        commitEdit(newMessage);
                }
                
            }
        }

        private static IEnumerable<string> SplitArguments(string line)
        {
            var builder = new StringBuilder();
            bool inQuotes = false;
            int argsReturned = 0;

            foreach (char c in line)
            {
                if (c == '"')
                    inQuotes = !inQuotes;
                else if (c == ' ' && !inQuotes && argsReturned < 3)
                {
                    yield return builder.ToString();
                    argsReturned++;
                    builder.Clear();
                }
                else
                    builder.Append(c);
            }
            if (builder.Length > 0)
                yield return builder.ToString();
        }

        public static void ReassignReNumber(BbsSession session, params string[] args)
        {
            var argNum = args.Length == 1 ? 0 : 1;
            int? newRe = null;

            if (args != null && argNum < args.Length)
            {
                if (int.TryParse(args[argNum], out int r))
                    newRe = r;
            }
            else
            {
                session.Io.Error(string.Join(Environment.NewLine, new[]
                {
                    "Usages:",
                    "/rere (msg #) (new re: number) - '/rere 123 120' sets 123's re: number to 120",
                    "/rere (msg #) none - '/rere 123 none' sets 123's re: number to nothing (a new thread)",
                    "/rere (new re: number) - '/rere 120' sets last read message's re: number to 120",
                    "/rere none - '/rere none' - sets last read message's re: number to nothing (a new thread)"
                }));
                return;
            }

            int? editId = null;
            if (args.Length == 1)
                editId = session.LastReadMessageNumber;
            else if (int.TryParse(args[0], out int n))
                editId = session.Chats.ItemKey(n);

            if (!editId.HasValue)
            {
                session.Io.Error("Usage: /rere (msg#) (new re: number)");
                return;
            }

            var msgNum = session.Chats.ItemNumber(editId);

            if (!msgNum.HasValue)
            {
                session.Io.Error("Cannot find the message to edit!");
                return;
            }

            Chat chat = session.Chats[editId.Value];

            if (chat == null)
            {
                session.Io.Error("Cannot find the message to edit!");
                return;
            }

            bool canUpdate =
                session.User.Access.HasFlag(AccessFlag.Administrator) ||
                session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                session.UcFlag.Flags.HasFlag(UCFlag.Moderator) ||
                (
                    chat.FromUserId == session.User.Id &&
                    (DateTime.UtcNow - chat.DateUtc).TotalMinutes <= Constants.MinutesUntilMessageIsUndeletable
                );

            if (!canUpdate)
            {
                session.Io.OutputLine($"Cannot edit message.  It's either too old (more than {Constants.MinutesUntilMessageIsUndeletable} minutes) or you aren't a moderator.");
                return;
            }


            int? reId = null;
            if (newRe.HasValue)
            {
                reId = session.Chats.ItemKey(newRe.Value);
                if (!reId.HasValue)
                {
                    session.Io.Error("Cannot find the message you're re:ferring to!");
                    return;
                }
            }

            chat.ResponseToId = reId;
            chat = DI.GetRepository<Chat>().Update(chat);
            session.Chats[chat.Id] = chat;

            session.Io.OutputLine($"Updated re: number for message {session.Chats.ItemNumber(chat.Id)} to {(newRe.HasValue ? newRe.ToString() : "nothing")}.");
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                chat.Write(session, ChatWriteFlags.Monochorome, GlobalDependencyResolver.Default);
            }
        }

        public static void CombineMessages(BbsSession session, params string[] args)
        {
            if (args == null || args.Length < 2 || !int.TryParse(args[0], out int msg1) || !int.TryParse(args[1], out int msg2) || msg1 == msg2)
            {
                var msg = string.Join(Environment.NewLine, new[]
                {
                    "Usage: '/combine n1 n2 [o]' where n1 is message number 1 and n2 is message number 2.",
                    "Optional argument [o] can specify separator between texts: 'nothing', 'space', 'newline', 'paragraph' (two newlines).",
                    "Default separator is '.  ' if message 1 ends with a letter or digit, otherwise nothing"
                });
                session.Io.Error(msg);
                return;
            }

            var small = Math.Min(msg1, msg2);
            var big = Math.Max(msg1, msg2);

            var key1 = session.Chats.ItemKey(small);
            var key2 = session.Chats.ItemKey(big);
            
            if (!key1.HasValue)
            {
                session.Io.Error($"Message {small} not found.");
                return;
            }

            if (!key2.HasValue)
            {
                session.Io.Error($"Message {big} not found.");
                return;
            }

            var chat1 = session.Chats[key1.Value];
            var chat2 = session.Chats[key2.Value];

            int? originalFlags = null;
            if (session.User.Access.HasFlag(AccessFlag.Administrator) && args.Any(a => "silent".Equals(a, StringComparison.CurrentCultureIgnoreCase)))
            {
                originalFlags = (int)session.ControlFlags;
                session.ControlFlags |= SessionControlFlags.DoNotSendNotifications;
            }
            
            CombineMessages(session, chat1, chat2, args.Skip(2).ToArray());

            session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, $"{session.User.Name} combined two messages into one:{Environment.NewLine}{session.Chats[key1.Value].Message}"));

            if (originalFlags.HasValue)
                session.ControlFlags = (SessionControlFlags)originalFlags.Value;
        }

        private static void CombineMessages(BbsSession session, Chat chat1, Chat chat2, params string[] args)
        {
            if (chat1.FromUserId != chat2.FromUserId)
            {
                session.Io.Error("Cannot combine chats from two different users.");
                return;
            }

            var mayCombine =
                session.User.Access.HasFlag(AccessFlag.Administrator) ||
                session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                session.UcFlag.Flags.HasFlag(UCFlag.Moderator) ||
                (chat1.FromUserId == session.User.Id &&
                (DateTime.UtcNow - chat1.DateUtc).TotalMinutes <= Constants.MinutesUntilMessageIsUndeletable &&
                (DateTime.UtcNow - chat2.DateUtc).TotalMinutes <= Constants.MinutesUntilMessageIsUndeletable);

            if (!mayCombine)
            {
                session.Io.Error("Access denied.");
                return;
            }

            var chatRepo = DI.GetRepository<Chat>();

            var separator = char.IsLetterOrDigit(chat1.Message.Last()) ? ".  " : string.Empty;
            if (args?.Length >= 1)
            {
                switch (args[0].ToLower())
                {
                    case "space":
                    case "\" \"":
                    case " ":
                        separator = " ";
                        break;
                    case "newline":
                    case "nl":
                    case "cr":
                    case "enter":
                        separator = Environment.NewLine;
                        break;
                    case "paragraph":
                    case "p":
                        separator = Environment.NewLine.Repeat(2);
                        break;
                    case "none":
                    case "nothing":
                        separator = string.Empty;
                        break;
                }
            }
            
            var text = $"{chat1.Message}{separator}{chat2.Message}";
            chat1.Message = text;
            chatRepo.Update(chat1);

            var originalFlags = session.ControlFlags;
            session.ControlFlags |= SessionControlFlags.DoNotSendNotifications;

            var chatsReferring = session.Chats
                .Values
                .Where(c => c.ResponseToId == chat2.Id)
                .ToList();

            session.Chats.Remove(chat2.Id);            
            chatRepo.Delete(chat2);

            if (true == chatsReferring?.Any())
            {
                var chat1Num = session.Chats.ItemNumber(chat1.Id).ToString();
                foreach (var c in chatsReferring)
                    ReassignReNumber(session, session.Chats.ItemNumber(c.Id).ToString(), chat1Num);
            }

            session.ControlFlags = originalFlags;
            if (session.LastMsgPointer == chat2.Id)
                session.LastMsgPointer = chat1.Id;
            if (session.MsgPointer == chat2.Id)
                session.MsgPointer = chat1.Id;
            if (session.LastReadMessageNumber == chat2.Id)
                session.LastReadMessageNumber = chat1.Id;
        }

        private static Chat FindChatToBeEdited(BbsSession session, params string[] args)
        {
            Chat toBeEdited = null;

            int n = -1;
            string msgNum = null;
            if (args.Length >= 1 && int.TryParse(args[0], out int _))
            {
                msgNum = args[0];
                args = args.Skip(1).ToArray();
            }

            if (!string.IsNullOrWhiteSpace(msgNum) && int.TryParse(msgNum, out n))
            {
                var nn = session.Chats.ItemKey(n);
                if (nn.HasValue)
                    toBeEdited = session.Chats[nn.Value];
            }
            else if (n <= 0)
                toBeEdited = session.Chats.Values.LastOrDefault(c => c.FromUserId == session.User.Id);

            return toBeEdited;
        }

        private static void ShowUsage(BbsSession session)
        {
            string msg = string.Join(Environment.NewLine, new[]
            {
                "Usage 1 (edit last chat)     : /typo \"(search)\" \"(replace)\"",
                "Usage 2 (edit specific chat) : /typo # \"(search)\" \"(replace)\"",
                "Usage 3 (edit last chat with line editor) : /edit",
                "Usage 4 (edit specific hat with line editor) : /edit 123",
                "Where # = the chat message number.",
                "Alternatively the quotes can be omitted if neither the search nor replace contain spaces.  If *either* the search or " +
                "the replace terms contain one or more spaces then *both* must be wrapped in quotes."
            });
            session.Io.OutputLine(msg);
        }
    }
}
