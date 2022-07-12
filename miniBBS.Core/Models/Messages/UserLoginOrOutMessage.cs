using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Data;
using System;

namespace miniBBS.Core.Models.Messages
{
    public class UserLoginOrOutMessage : IMessage
    {
        public UserLoginOrOutMessage(User user, Guid sessionId, bool isLogin)
        {
            _userRef = new WeakReference(user);
            SessionId = sessionId;
            IsLogin = isLogin;
        }

        public Guid SessionId { get; private set; }
        private WeakReference _userRef = null;
        public User User => _userRef.Target as User;
        public bool IsLogin { get; private set; }
    }
}
