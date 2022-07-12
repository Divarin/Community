using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Messages;
using System;

namespace miniBBS.Subscribers
{
    public class EmoteSubscriber : ISubscriber<EmoteMessage>
    {
        public Action<EmoteMessage> OnMessageReceived { get; set; }

        public void Receive(EmoteMessage message)
        {
            OnMessageReceived?.Invoke(message);
        }
    }
}
