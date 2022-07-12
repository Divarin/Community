using System;

namespace miniBBS.Core.Enums
{
    [Flags]
    public enum AccessFlag
    {
        /// <summary>
        /// No access (account suspended)
        /// </summary>
        None = 0,

        /// <summary>
        /// Normal access (may log in)
        /// </summary>
        MayLogon = 1,

        /// <summary>
        /// Can access any channel regardless of user/channel access flags
        /// </summary>
        GlobalModerator = 2,

        /// <summary>
        /// System administrator, can shut-down system and perform other various maintenence tasks.
        /// </summary>
        Administrator = 4,
    }
}
