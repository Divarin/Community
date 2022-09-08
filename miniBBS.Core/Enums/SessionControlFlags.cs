using System;

namespace miniBBS.Core.Enums
{
    [Flags]
    public enum SessionControlFlags
    {
        None = 0,
        DoNotSendNotifications = 1,
        Invisible = 2,
    }
}
