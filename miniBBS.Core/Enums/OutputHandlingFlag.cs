using System;

namespace miniBBS.Core.Enums
{
    [Flags]
    public enum OutputHandlingFlag
    {
        None = 0,
        Nonstop = 1,
        DoNotTrimStart = 2,
        PauseAtEnd = 4,
        NoWordWrap = 8,
        SplitOnNewlineOnly = 16,

        /// <summary>
        /// For reading a saved bookmark, advances to the line where the bookmark was created
        /// </summary>
        AdvanceToPercentage = 32,
    }
}
