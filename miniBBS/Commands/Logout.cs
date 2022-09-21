using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Commands
{
    public static class Logout
    {
        public static bool Execute(BbsSession session, string command, string quitMessage)
        {
            bool quickLogout = "/o".Equals(command, StringComparison.CurrentCultureIgnoreCase);
            if (!quickLogout)
            {
                if ('Y' != session.Io.Ask("Log out?"))
                    return false;
            }

            session.Items[SessionItem.LogoutMessage] = quitMessage;
            return true;
        }
    }
}
