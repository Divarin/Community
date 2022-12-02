using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;

namespace miniBBS.Services.GlobalCommands
{
    public static class Tutor
    {
        public static void Execute(BbsSession session, string msg)
        {
            // We'll only "tute" users that are "new" (< Constants.TutorLogins total logins)
            if (session.User?.TotalLogons >= Constants.TutorLogins)
                return;

            HashSet<string> shown;
            if (session.Items.ContainsKey(SessionItem.ShownTutorMessages))
                shown = session.Items[SessionItem.ShownTutorMessages] as HashSet<string>;
            else
            {
                shown = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
                session.Items[SessionItem.ShownTutorMessages] = shown;
            }

            if (shown.Add(msg))
                session.Io.Error($"Tutor:{Environment.NewLine}{msg}");
        }
    }
}
