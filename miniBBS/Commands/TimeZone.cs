using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;

namespace miniBBS.Commands
{
    public static class TimeZone
    {
        public static void Execute(BbsSession session, string adjust = null)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                if (string.IsNullOrWhiteSpace(adjust) || !int.TryParse(adjust, out var tz) || Math.Abs(tz) > 23)
                    adjust = AskForAdjustment(session);
                if (string.IsNullOrWhiteSpace(adjust) || !int.TryParse(adjust, out tz) || Math.Abs(tz) > 23)
                {
                    session.Io.Error("Time Zone not changed.");
                    return;
                }
                session.TimeZone = tz;
                session.Io.OutputLine($"Time Zone Adjustment: UTC {tz} hours.{(session.Cols >= 80 ? " " : session.Io.NewLine)}Can be changed in preferences.");
                if (session.User.Timezone != tz)
                {
                    session.User.Timezone = tz;
                    session.UserRepo.Update(session.User);
                }
            }
        }

        private static string AskForAdjustment(BbsSession session)
        {
            var utcNow = DateTime.UtcNow;
            session.Io.OutputLine($"Currently times are being adjusted from UTC by {session.TimeZone} hours.");
            session.Io.OutputLine($"Current Time UTC:      {utcNow:HH:mm:ss}");
            session.Io.OutputLine($"Current Time Adjusted: {utcNow.AddHours(session.TimeZone):HH:mm:ss}");
            session.Io.Output("Time Zone Offet (-23 to 23): ".Color(ConsoleColor.Yellow));
            return session.Io.InputLine()?.Trim();
        }
    }
}
