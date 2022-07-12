using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Messages;
using System;

namespace miniBBS.Subscribers
{
    public class ChannelMessageSubscriber : ISubscriber<ChannelMessage>
    {
        private readonly Guid _sessionId;

        public ChannelMessageSubscriber(Guid sessionId)
        {
            _sessionId = sessionId;
        }

        public Action<ChannelMessage> OnMessageReceived { get; set; }

        public void Receive(ChannelMessage message)
        {
            if (_sessionId.Equals(message?.SessionId))
                return;
            OnMessageReceived?.Invoke(message);
        }
    }
}
