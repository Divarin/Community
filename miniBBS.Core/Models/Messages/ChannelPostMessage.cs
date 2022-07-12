using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Data;
using System;

namespace miniBBS.Core.Models.Messages
{
    public class ChannelPostMessage : IMessage
    {
        public ChannelPostMessage(Chat chat, Guid sessionId)
        {
            _chatRef = new WeakReference(chat);
            SessionId = sessionId;
        }

        private WeakReference _chatRef = null;
        public Chat Chat => _chatRef?.Target as Chat;
        public Guid SessionId { get; private set; }
    }
}
