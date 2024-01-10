using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using System;
using System.Linq;

namespace miniBBS.Subscribers
{
    public class ChannelMessageSubscriber : ISubscriber<ChannelMessage>
    {
        private readonly Guid _sessionId;
        private WeakReference _mySessionRef;

        public ChannelMessageSubscriber(Guid sessionId)
        {
            _sessionId = sessionId;
        }

        public Action<ChannelMessage> OnMessageReceived { get; set; }

        public void Receive(ChannelMessage message)
        {
            if (_sessionId.Equals(message?.SessionId))
                return;
            if (message.Predicate != null)
            {
                var mySession = GetSession();
                if (!message.Predicate.Invoke(mySession))
                    return;
            }
            OnMessageReceived?.Invoke(message);
        }

        private BbsSession GetSession()
        {
            if (_mySessionRef != null && _mySessionRef.IsAlive && _mySessionRef.Target is BbsSession)
                return _mySessionRef.Target as BbsSession;

            var mySession = DI.Get<ISessionsList>()
                    .Sessions
                    .FirstOrDefault(s => s.Id == _sessionId);
            _mySessionRef = new WeakReference(mySession);
            return mySession;
        }
    }
}
