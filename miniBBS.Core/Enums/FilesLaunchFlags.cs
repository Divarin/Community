using System;

namespace miniBBS.Core.Enums
{
    [Flags]
    public enum FilesLaunchFlags
    {
        None = 0,
        MoveToUserHomeDirectory = 1,
        ReturnToPreviousDirectory = 2
    }
}
