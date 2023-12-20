using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Core.Models.Messages
{
    public class NullSpaceMessage : IMessage
    {
        public NullSpaceMessage(BbsSession session, string message)
        {
            SessionId = session.Id;
            Message = message;
        }

        public Guid SessionId { get; private set; }

        public string Message { get; private set; }
    }
}
