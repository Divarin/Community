using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Core.Models.Control
{
    /// <summary>
    /// An in-memory queue of requests to get voice in a channel.  Typically used during interview sessions.
    /// </summary>
    public class VoiceRequestQueue
    {
        private readonly Queue<int> _requestingUserIdQueue = new Queue<int>();

        public VoiceRequestQueue(int channelId, TimeSpan duration)
        {
            ChannelId = channelId;
            ExpiresAtUtc = DateTime.UtcNow + duration;
        }

        public int ChannelId { get; private set; }        
        public DateTime ExpiresAtUtc { get; private set; }
        public Action OnExpire { get; set; }
        public int Count => _requestingUserIdQueue?.Count ?? 0;

        /// <summary>
        /// Extends the expiration by <paramref name="duration"/>
        /// </summary>
        public void Extend(TimeSpan duration)
        {
            ExpiresAtUtc += duration;
        }

        public void Enqueue(int userId)
        {
            if (!_requestingUserIdQueue.Contains(userId))
                _requestingUserIdQueue.Enqueue(userId);
        }

        public int? Dequeue()
        {
            if (_requestingUserIdQueue.Count > 0)
                return _requestingUserIdQueue.Dequeue();
            return null;
        }

        public IEnumerable<int> PeekAll()
        {
            return _requestingUserIdQueue.AsEnumerable().ToList();
        }

        public int? Peek()
        {
            if (_requestingUserIdQueue.Count > 0)
                return _requestingUserIdQueue.Peek();
            return null;
        }
    }

}
