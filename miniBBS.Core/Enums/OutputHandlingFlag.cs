using System;

namespace miniBBS.Core.Enums
{
    [Flags]
    public enum OutputHandlingFlag
    {
        None = 0,
        Nonstop = 1,
        DoNotTrimStart = 2,
        PauseAtEnd = 4
    }
}
