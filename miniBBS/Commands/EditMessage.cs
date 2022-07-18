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
                Chat toBeEdited = null;
                int n = -1;
                string msgNum = args?.Length >= 3 ? args[0] : null;
                string search = args?.Length >= 3 ? args[1] : args[0];
                string replace = args?.Length >= 3 ? args[2] : args[1];

                if (string.IsNullOrWhiteSpace(search) || string.IsNullOrWhiteSpace(replace))
                {
                    session.Io.OutputLine("Usage 1 (edit last chat)     : /edit (search) (replace)");
                    session.Io.OutputLine("Usage 2 (edit specific chat) : /edit # (search) (replace)");
                    return;
                }

                search = search.Replace('-', ' ');
                replace = replace.Replace('-', ' ');

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
    }
}
