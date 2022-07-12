using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Messages;
using System;

namespace miniBBS.Subscribers
{
    public class UserLoginOrOutSubscriber : ISubscriber<UserLoginOrOutMessage>
    {
        private readonly Guid _sessionId;

        public UserLoginOrOutSubscriber(Guid sessionId)
        {
            _sessionId = sessionId;
        }
        public Action<UserLoginOrOutMessage> OnMessageReceived { get; set; }

        public void Receive(UserLoginOrOutMessage message)
        {
            if (message.User == null || _sessionId.Equals(message?.SessionId))
                return;
            OnMessageReceived?.Invoke(message);
        }
    }
}
