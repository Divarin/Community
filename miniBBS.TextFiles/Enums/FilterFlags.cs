using System;

namespace miniBBS.TextFiles.Enums
{
    [Flags]
    public enum FilterFlags
    {
        None = 0,
        Filename = 1,
        Description = 2,
        Contents = 4
    }
}
