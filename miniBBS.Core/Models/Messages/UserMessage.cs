using miniBBS.Core.Interfaces;
using System;

namespace miniBBS.Core.Models.Messages
{
    public class UserMessage : IMessage
    {
        public UserMessage(Guid sessionId, int userId, string message)
        {
            SessionId = sessionId;
            UserId = userId;
            Message = message;
        }

        public Guid SessionId { get; private set; }
        public int UserId { get; set; }
        public string Message { get; set; }
    }
}
