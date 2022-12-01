using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions_Model;
using System;
using System.Linq;

namespace miniBBS.Services.GlobalCommands
{
    public static class ShowNextMessage
    {
        public static Chat Execute(BbsSession session, ChatWriteFlags chatWriteFlags)
        {
            if (!session.Chats.ContainsKey(session.MsgPointer))
            {
                if (!chatWriteFlags.HasFlag(ChatWriteFlags.FormatForMessageBase))
                {
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
                    {
                        session.Io.OutputLine("No more messages in this channel.");
                    }
                }

                var key = session.LastMsgPointer ?? session.Chats?.Keys?.Last();
                if (key == null || true != session.Chats?.ContainsKey(key.Value))
                    return null;
                return session.Chats[key.Value];
            }
            else
            {
                Chat nextMessage = session.Chats[session.MsgPointer];
                nextMessage.Write(session, chatWriteFlags, GlobalDependencyResolver.Default);

                if (!SetMessagePointer.Execute(session, session.MsgPointer + 1) && !chatWriteFlags.HasFlag(ChatWriteFlags.FormatForMessageBase))
                {
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                    {
                        session.Io.OutputLine("No more messages in this channel.");
                    }
                }

                return nextMessage;
            }
        }
    }
}
