using System;

namespace miniBBS.Core.Models.Control
{
    public class LoginRecord
    {
        public Guid SessionId { get; set; }
        public DateTime LoginAtLocal { get; set; }
        public DateTime? LogoutAtLocal { get; set; }
        public TimeSpan LoginDuration => (LogoutAtLocal ?? DateTime.Now) - LoginAtLocal;
        public string Username { get; set; }
        public string IpAddress { get; set; }

        public override string ToString()
        {
            // 12345678901234567890123456789012345678901234567890123456789012345678901234567890
            // Coolbeans 10-31 08:18 - 10-31:09:19 (1h 1m 0s) 255.255.255.255
            return $"{Username} {LoginAtLocal:MM-dd HH:mm}-{(LogoutAtLocal ?? DateTime.Now):MM-dd HH:mm} ({LoginDuration.Hours}h {LoginDuration.Minutes}m {LoginDuration.Seconds}s) {IpAddress}";
        }
    }
}
