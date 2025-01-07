using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Linq;

namespace miniBBS.Services.GlobalCommands
{
    public static class ShowNextMessage
    {
        public static Chat Execute(BbsSession session, ChatWriteFlags chatWriteFlags)
        {
            // can happen if user was reading old/archived posts then switched the archive filter back on.
            if (session.Chats?.Any() == true && session.MsgPointer < session.Chats.Keys.First())
            {
                session.MsgPointer = session.Chats.Keys.First();
            }

            if (!session.Chats.ContainsKey(session.MsgPointer))
            {
                if (!chatWriteFlags.HasFlag(ChatWriteFlags.FormatForMessageBase) && !chatWriteFlags.HasFlag(ChatWriteFlags.DoNotShowMessage))
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

                if (!SetMessagePointer.Execute(session, session.MsgPointer + 1) &&
                    !chatWriteFlags.HasFlag(ChatWriteFlags.FormatForMessageBase) &&
                    !chatWriteFlags.HasFlag(ChatWriteFlags.DoNotShowMessage))
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
