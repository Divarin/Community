using System;

namespace miniBBS.Core.Enums
{
    [Flags]
    public enum ChatWriteFlags
    {
        None = 0,
        UpdateLastReadMessage = 1,
        UpdateLastMessagePointer = 2,
        Monochorome = 4,
        FormatForMessageBase = 8,
        LiveMessageNotification = 16
    }
}
