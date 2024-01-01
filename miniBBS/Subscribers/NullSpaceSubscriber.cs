using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
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
            var monochrome =
                session.Io.EmulationType == TerminalEmulation.Ascii ||
                session.Io.EmulationType == TerminalEmulation.Atascii;

            if (message.SessionId != session.Id)
            {
                var msg = message.Message;
                if (monochrome)
                    session.Io.Output(msg);
                else
                    session.Io.Output(msg.Color(message.KeyColor));
            }
        }
    }
}
