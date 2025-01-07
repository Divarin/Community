using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using miniBBS.Services.GlobalCommands;
using miniBBS.Services.Persistence;
using System;
using System.Linq;

namespace miniBBS.Commands
{
    public static class DeleteMessage
    {
        public static void Execute(BbsSession session, string msgNum = null)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
            {
                Chat toBeDeleted = null;
                int n = -1;
                if (!string.IsNullOrWhiteSpace(msgNum) && int.TryParse(msgNum, out n))
                {
                    var nn = session.Chats.ItemKey(n);
                    if (nn.HasValue && session.Chats.ContainsKey(nn.Value))
                        toBeDeleted = session.Chats[nn.Value];
                }
                else if (n <= 0)
                    toBeDeleted = session.Chats.Values.LastOrDefault(c => c.FromUserId == session.User.Id);

                if (toBeDeleted == null)
                {
                    session.Io.OutputLine("Message not found in this channel.");
                    return;
                }

                bool canDelete =
                    session.User.Access.HasFlag(AccessFlag.Administrator) ||
                    session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                    session.UcFlag.Flags.HasFlag(UCFlag.Moderator) ||
                    (
                        toBeDeleted.FromUserId == session.User.Id &&
                        (DateTime.UtcNow - toBeDeleted.DateUtc).TotalMinutes  <= Constants.MinutesUntilMessageIsUndeletable
                    );

                if (!canDelete)
                    session.Io.OutputLine($"Cannot delete message.  It's either too old (more than {Constants.MinutesUntilMessageIsUndeletable} minutes) and you are not a moderator.");
                else
                {
                    session.Io.SetForeground(ConsoleColor.White);
                    session.Io.OutputLine(toBeDeleted.Message);
                    session.Io.SetForeground(ConsoleColor.Red);
                    var k = session.Io.Ask("Delete this? ");
                    if (k == 'Y')
                    {
                        string message = $"{session.User.Name} deleted a message from channel {session.Channel.Name}";
                        session.Chats.Remove(toBeDeleted.Id);
                        
                        DI.GetRepository<Chat>().Delete(toBeDeleted);
                        DI.Get<IChatCache>().DeleteChat(toBeDeleted);
                        session.Io.OutputLine("Done.");

                        session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, message)
                        {
                            OnReceive = (s) =>
                            {
                                s.Chats.Remove(toBeDeleted.Id);
                                SetMessagePointer.Execute(s, s.MsgPointer);
                            }
                        });
                        DI.Get<ILogger>().Log(session, message);
                        SetMessagePointer.Execute(session, session.MsgPointer);
                    }
                }
            }
        }
    }
}
