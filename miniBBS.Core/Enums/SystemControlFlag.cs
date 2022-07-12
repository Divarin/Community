using System;

namespace miniBBS.Core.Enums
{
    [Flags]
    public enum SystemControlFlag
    {
        /// <summary>
        /// No flags (normal operation)
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Only the system administator may logon
        /// </summary>
        AdministratorLoginOnly = 1,

        /// <summary>
        /// The system is shutting down
        /// </summary>
        Shutdown = 2
    }
}
