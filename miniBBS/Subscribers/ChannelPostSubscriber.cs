using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Messages;
using System;

namespace miniBBS.Subscribers
{
    public class ChannelPostSubscriber : ISubscriber<ChannelPostMessage>
    {
        private readonly Guid _sessionId;

        public ChannelPostSubscriber(Guid sessionId)
        {
            _sessionId = sessionId;
        }

        public void Receive(ChannelPostMessage message)
        {
            if (message.Chat == null || _sessionId.Equals(message?.SessionId)) 
                return;
            OnMessageReceived?.Invoke(message);
        }

        public Action<ChannelPostMessage> OnMessageReceived { get; set; }
    }
}
