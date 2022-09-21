using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using System;

namespace miniBBS.Core.Models.Messages
{
    public class UserLoginOrOutMessage : IMessage
    {
        public UserLoginOrOutMessage(BbsSession session, bool isLogin)
        {
            _userRef = new WeakReference(session.User);
            SessionId = session.Id;
            IsLogin = isLogin;
            LogoutMessage = session.Items.ContainsKey(SessionItem.LogoutMessage) ? session.Items[SessionItem.LogoutMessage] as string : null;
        }

        public Guid SessionId { get; private set; }
        private readonly WeakReference _userRef = null;
        public User User => _userRef.Target as User;
        public bool IsLogin { get; private set; }
        public string LogoutMessage { get; set; }
    }
}
