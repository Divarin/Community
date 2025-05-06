using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Subscribers
{
    public class NewUserRegisteredSubscriber : ISubscriber<NewUserRegisteredMessage>
    {
        private readonly WeakReference _sessionReference;

        public NewUserRegisteredSubscriber(BbsSession session)
        {
            _sessionReference = new WeakReference(session);
        }

        public Action<NewUserRegisteredMessage> OnMessageReceived { get; set; }

        public void Receive(NewUserRegisteredMessage message)
        {
            var mySession = (_sessionReference?.IsAlive ?? false) ? _sessionReference.Target as BbsSession : null;
            if (mySession == null)
                return;

            var username = message.Username;
            if (string.IsNullOrWhiteSpace(username))
                return;

            if (mySession.Usernames.ContainsKey(message.UserId))
                return;

            mySession.Usernames[message.UserId] = username;

            OnMessageReceived?.Invoke(message);
        }
    }
}
