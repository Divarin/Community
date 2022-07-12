using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Core.Models.Messages
{
    /// <summary>
    /// A generic message that should be seen by anyone currently in the channel
    /// </summary>
    public class ChannelMessage : IMessage
    {
        public ChannelMessage(Guid sessionId, int channelId, string message)
        {
            SessionId = sessionId;
            ChannelId = channelId;
            Message = message;
        }

        public int ChannelId { get; private set; }
        public string Message { get; private set; }
        public Guid SessionId { get; private set; }
        public Action<BbsSession> OnReceive { get; set; }
    }
}
