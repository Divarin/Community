using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Messages;
using System;

namespace miniBBS.Subscribers
{
    public class UserMessageSuibscriber : ISubscriber<UserMessage>
    {
        private readonly Guid _sessionId;
        private readonly int _userId;

        public Action<UserMessage> OnMessageReceived { get; set; }

        public UserMessageSuibscriber(Guid sessionId, int userId)
        {
            _sessionId = sessionId;
            _userId = userId;
        }

        public void Receive(UserMessage message)
        {
            if (_sessionId.Equals(message?.SessionId) || message.UserId != _userId)
                return;
            OnMessageReceived?.Invoke(message);
        }
    }
}
