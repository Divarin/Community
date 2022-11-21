using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;

namespace miniBBS.Services.GlobalCommands
{
    public static class ShowNextMessage
    {
        public static void Execute(BbsSession session, ChatWriteFlags chatWriteFlags)
        {
            if (!session.Chats.ContainsKey(session.MsgPointer))
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
                {
                    session.Io.OutputLine("No more messages in this channel.");
                }
            }
            else
            {
                Chat nextMessage = session.Chats[session.MsgPointer];
                nextMessage.Write(session, chatWriteFlags);

                if (!SetMessagePointer.Execute(session, session.MsgPointer + 1))
                {
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                    {
                        session.Io.OutputLine("No more messages in this channel.");
                    }
                }
            }
        }
    }
}
