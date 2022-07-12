using System;

namespace miniBBS.Core.Enums
{
    [Flags]
    public enum UCFlag
    {
        /// <summary>
        /// No flags
        /// </summary>
        None = 0,

        /// <summary>
        /// User is invited to join the channel (if it requires an invite)
        /// </summary>
        Invited = 1,

        /// <summary>
        /// If user can join the channel, they can read but cannot write messages
        /// </summary>
        ReadyOnly = 2,

        /// <summary>
        /// User is moderator and may delete messages, invite members, kick & ban users
        /// </summary>
        Moderator = 4
    }
}
