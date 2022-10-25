using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Extensions
{
    public static class SessionExtensions
    {
        public static HashSet<string> IgnoreList(this BbsSession session)
        {
            if (session.Items.ContainsKey(SessionItem.IgnoreList))
                return session.Items[SessionItem.IgnoreList] as HashSet<string>;
            return null;
        }

        public static bool IsIgnoring(this BbsSession session, string username)
        {
            return true == session.IgnoreList()?.Contains(username, StringComparer.CurrentCultureIgnoreCase);
        }

        public static bool IsIgnoring(this BbsSession session, int userId)
        {
            var username = session.Usernames.ContainsKey(userId) ? session.Usernames[userId] : null;
            return username != null && session.IsIgnoring(username);
        }
    }
}
