using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Core.Models.Messages
{
    public class NewUserRegisteredMessage : IMessage
    {
        public NewUserRegisteredMessage(BbsSession session)
        {
            Username = session.User.Name;
            UserId = session.User.Id;
            SessionId = session.Id;
        }

        public string Username { get; private set; }

        public int UserId { get; private set; }

        public Guid SessionId { get; private set; }
    }
}
