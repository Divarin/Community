using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Messages;
using System;

namespace miniBBS.Subscribers
{
    public class GlobalMessageSubscriber : ISubscriber<GlobalMessage>
    {
        private readonly Guid _sessionId;

        public GlobalMessageSubscriber(Guid sessionId)
        {
            _sessionId = sessionId;
        }

        public Action<GlobalMessage> OnMessageReceived { get; set; }

        public void Receive(GlobalMessage message)
        {
            if (_sessionId.Equals(message?.SessionId))
                return;
            OnMessageReceived?.Invoke(message);
        }
    }
}
