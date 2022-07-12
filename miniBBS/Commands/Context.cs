using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Linq;

namespace miniBBS.Commands
{
    public static class Context
    {
        public static void Execute(BbsSession session, string arg=null)
        {
            int? currentMessage = session.ContextPointer ?? session.LastReadMessageNumber;
            if (!currentMessage.HasValue || true != session.Chats?.ContainsKey(currentMessage.Value))
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    session.Io.OutputLine("Unable to locate the message you last read.");
                    return;
                }
            }

            var currentChat = session.Chats[currentMessage.Value];
            Chat reChat = null;
            if (arg == ">")
            {
                reChat = session.Chats.Values.FirstOrDefault(c => c.ResponseToId == currentChat.Id);
                if (reChat == null)
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                    {
                        session.Io.OutputLine($"Message {session.Chats.ItemNumber(currentChat.Id)} does not appear to have a response.");
                        return;
                    }
            }
            else if (!currentChat.ResponseToId.HasValue || !session.Chats.ContainsKey(currentChat.ResponseToId.Value))
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    session.Io.OutputLine($"Message {session.Chats.ItemNumber(currentChat.Id)} does not appear to be in response to another message or that message has been deleted.");
                    return;
                }
            }

            if (arg != ">")
                reChat = session.Chats[currentChat.ResponseToId.Value];

            if (arg == "0")
            {
                while (reChat.ResponseToId.HasValue && session.Chats.ContainsKey(reChat.ResponseToId.Value))
                {
                    reChat = session.Chats[reChat.ResponseToId.Value];
                }
            }
            else if (arg == "e" || arg == "E")
            {
                do
                {
                    var nextChat = session.Chats.Values.FirstOrDefault(c => c.ResponseToId == reChat.Id);
                    if (nextChat == null)
                        break;
                    reChat = nextChat;
                } while (true);
            }
            var color = session.Io.GetForeground();
            session.Io.SetForeground(ConsoleColor.Yellow);
            session.Io.OutputLine($"Recalling message {session.Chats.ItemNumber(reChat.Id)} to provide context for message {session.Chats.ItemNumber(currentChat.Id)}:");
            session.Io.SetForeground(ConsoleColor.Magenta);
            reChat.Write(session, updateLastReadMessageNumber: false, monochrome: true);
            session.Io.SetForeground(color);

            session.ContextPointer = reChat.Id;
        }
    }
}
