using System;

namespace miniBBS.Core.Enums
{
    [Flags]
    public enum LoggingOptions
    {
        None = 0,
        ToConsole = 1,
        ToDatabase = 2,
        WriteImmedately = 4
    }
}
