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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="channelId"></param>
        /// <param name="message"></param>
        /// <param name="disturb">If true then message will be shown even to users with DoNotDistrub turned on</param>
        public ChannelMessage(Guid sessionId, int channelId, string message, bool disturb=false, Func<BbsSession, bool> predicate = null)
        {
            SessionId = sessionId;
            ChannelId = channelId;
            Message = message;
            Disturb = disturb;
            Predicate = predicate;
        }

        public int ChannelId { get; private set; }
        public string Message { get; private set; }
        public bool Disturb { get; private set; }
        public Guid SessionId { get; private set; }
        public Action<BbsSession> OnReceive { get; set; }
        public Func<BbsSession, bool> Predicate { get; set; }
    }
}
