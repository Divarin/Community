using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Commands
{
    public static class TimeZone
    {
        public static void Execute(BbsSession session, string adjust)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                int tz;
                if (string.IsNullOrWhiteSpace(adjust) || !int.TryParse(adjust, out tz) || Math.Abs(tz) > 23)
                {
                    int et = (int) (DateTime.Now - DateTime.UtcNow).TotalHours;
                    string msg = 
                        $"Currently times are being adjusted from UTC by {session.TimeZone} hours.  Use /tz (hours) to change. " +
                        $"For example to show times in UTC use '/tz 0'  To show times in Eastern Time Zone use '/tz {et}'.";
                    session.Io.OutputLine(msg);
                    return;
                }
                session.TimeZone = tz;
                session.Io.OutputLine($"Times will now be offset from UTC by {tz} hours.  Use '/tz 0' to undo this and go back to showing times in UTC.");
                if (session.User.Timezone != tz)
                {
                    session.User.Timezone = tz;
                    session.UserRepo.Update(session.User);
                }
            }
        }
    }
}
