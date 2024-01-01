using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Core.Models.Messages
{
    public class NullSpaceMessage : IMessage
    {
        public NullSpaceMessage(BbsSession session, ConsoleColor keyColor, string message)
        {
            SessionId = session.Id;
            KeyColor = keyColor;
            Message = message;
        }

        public Guid SessionId { get; private set; }

        public ConsoleColor KeyColor { get; private set; }

        public string Message { get; private set; }
    }
}
