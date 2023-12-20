using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using System;

namespace miniBBS.Subscribers
{
    public class NullSpaceSubscriber : ISubscriber<NullSpaceMessage>
    {
        public Action<NullSpaceMessage> OnMessageReceived { get; set; }
        private readonly WeakReference _sessionRef;

        public NullSpaceSubscriber(BbsSession session)
        {
            _sessionRef = new WeakReference(session);
        }

        public void Receive(NullSpaceMessage message)
        {
            if (_sessionRef == null || !_sessionRef.IsAlive || _sessionRef.Target == null)
                return;
            
            var session = _sessionRef.Target as BbsSession;
            if (message.SessionId != session.Id)
            {
                var msg = message.Message;
                session.Io.Output(msg);
            }
        }
    }
}
