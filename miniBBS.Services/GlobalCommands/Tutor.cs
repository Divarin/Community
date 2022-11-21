using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;

namespace miniBBS.Services.GlobalCommands
{
    public static class Tutor
    {
        public static void Execute(BbsSession session, string msg)
        {
            // We'll only "tute" users that are "new" (< Constants.TutorLogins total logins)
            if (session.User?.TotalLogons >= Constants.TutorLogins)
                return;

            session.Io.Error(msg);
        }
    }
}
