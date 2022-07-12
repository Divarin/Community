using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Core.Models.Messages
{
    public class EmoteMessage : IMessage
    {
        public EmoteMessage(Guid sessionId, int fromUserId, int channelId, int? targetUserId, string message)
        {
            SessionId = sessionId;
            ChannelId = channelId;
            FromUserId = fromUserId;
            TargetUserId = targetUserId;
            Message = message;
        }

        public int ChannelId { get; private set; }
        public int FromUserId { get; private set; }
        public int? TargetUserId { get; private set; }
        public string Message { get; private set; }
        public Guid SessionId { get; private set; }
        public Action<BbsSession> OnReceive { get; set; }
    }
}
