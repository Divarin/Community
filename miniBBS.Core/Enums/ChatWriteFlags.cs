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
        LiveMessageNotification = 16,

        /// <summary>
        /// Used when you want only the side-effects such as updating read message, last message pointer but don't actually show the message.
        /// </summary>
        DoNotShowMessage = 32
    }
}
