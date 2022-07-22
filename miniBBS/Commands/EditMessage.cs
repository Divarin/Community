using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using System;
using System.Linq;

namespace miniBBS.Commands
{
    public static class EditMessage
    {
        public static void Execute(BbsSession session, string[] args)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
            {
                if (args?.Length < 2)
                {
                    ShowUsage(session);
                    return;
                }

                Chat toBeEdited = null;
                int n = -1;
                string msgNum = null;
                if (args.Length >= 3 && int.TryParse(args[0], out int _))
                {
                    msgNum = args[0];
                    args = args.Skip(1).ToArray();
                }

                string search = args[0];
                string replace = args[1];

                string line = string.Join(" ", args);
                bool parsedWithSpaceDelim = true;
                if (line.Count(c => c == '"') == 4)
                {
                    var parts = line
                        .Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToArray();
                    if (parts?.Length == 2)
                    {
                        search = parts[0];
                        replace = parts[1];
                        parsedWithSpaceDelim = false;
                    }
                } else if (line.Count(c => c == '/') == 1)
                {
                    var parts = line
                        .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToArray();
                    if (parts?.Length == 2)
                    {
                        search = parts[0];
                        replace = parts[1];
                        parsedWithSpaceDelim = false;
                    }
                }

                if (string.IsNullOrWhiteSpace(search) || string.IsNullOrWhiteSpace(replace))
                {
                    ShowUsage(session);
                    return;
                }

                if (parsedWithSpaceDelim)
                {
                    search = search.Replace('-', ' ');
                    replace = replace.Replace('-', ' ');
                }

                if (!string.IsNullOrWhiteSpace(msgNum) && int.TryParse(msgNum, out n))
                {
                    var nn = session.Chats.ItemKey(n);
                    if (nn.HasValue)
                        toBeEdited = session.Chats[nn.Value];
                }
                else if (n <= 0)
                    toBeEdited = session.Chats.Values.LastOrDefault(c => c.FromUserId == session.User.Id);

                if (toBeEdited == null)
                    session.Io.OutputLine("Messagee not found in this channel.");

                bool canDelete =
                    session.User.Access.HasFlag(AccessFlag.Administrator) ||
                    session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                    session.UcFlag.Flags.HasFlag(UCFlag.Moderator) ||
                    (
                        toBeEdited.FromUserId == session.User.Id &&
                        (DateTime.UtcNow - toBeEdited.DateUtc).TotalMinutes <= Constants.MinutesUntilMessageIsUndeletable
                    );

                if (!canDelete)
                    session.Io.OutputLine($"Cannot edit message.  It's either too old (more than {Constants.MinutesUntilMessageIsUndeletable} minues) or you aren't a moderator.");
                else
                {
                    string newMessage = toBeEdited.Message.Replace(search, replace);
                    session.Io.SetForeground(ConsoleColor.White);
                    session.Io.OutputLine(newMessage);
                    session.Io.SetForeground(ConsoleColor.Red);
                    session.Io.Output("Make this edit? ");
                    var k = session.Io.InputKey();
                    session.Io.OutputLine();
                    if (k == 'y' || k == 'Y')
                    {
                        string message = $"{session.User.Name} edited message # {session.Chats.ItemNumber(toBeEdited.Id)} in channel {session.Channel.Name}";
                        toBeEdited.Message = newMessage;
                        session.Io.OutputLine("Done.");
                        DI.GetRepository<Chat>().Update(toBeEdited);
                        session.Messager.Publish(new ChannelMessage(session.Id, session.Channel.Id, message)
                        {
                            OnReceive = (s) => s.Chats[toBeEdited.Id].Message = newMessage
                        });
                        DI.Get<ILogger>().Log(message);
                    }
                }
            }
        }

        private static void ShowUsage(BbsSession session)
        {
            string msg = string.Join(Environment.NewLine, new[]
            {
                "Usage 1 (edit last chat)     : /edit \"(search)\" \"(replace)\"",
                "Usage 2 (edit specific chat) : /edit # \"(search)\" \"(replace)\"",
                "Where # = the chat message number.",
                "Alternatively the quotes can be omitted if neither the search nor replace contain spaces.  If *either* the search or " +
                "the replace terms contain one or more spaces then *both* must be wrapped in quotes."
            });
            session.Io.OutputLine(msg);
        }
    }
}
