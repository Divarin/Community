using System;

namespace miniBBS.Core.Enums
{
    /// <summary>
    /// Flags that control how the TextFiles browser works during a user's session
    /// </summary>
    [Flags]
    public enum TextFilesSessionFlags
    {
        None = 0,
        ShowBackupFiles = 1
    }
}
