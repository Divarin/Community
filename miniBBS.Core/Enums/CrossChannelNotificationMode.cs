using System;

namespace miniBBS.Core.Enums
{
    [Flags]
    public enum CrossChannelNotificationMode
    {
        /// <summary>
        /// Never notifiy me of new messages in other channels
        /// </summary>
        None = 0,

        /// <summary>
        /// Only notify me if the post is in response to a message I posted
        /// </summary>
        PostIsInResponseToMyMessage = 1,

        /// <summary>
        /// Only notify me if the post mentions me by name
        /// </summary>
        PostMentionsMe = 2,

        /// <summary>
        /// Notify me of any new messages in other channels
        /// </summary>
        Any = 4,

        /// <summary>
        /// Only notify me of the first new message in any given channel,
        /// subsequent new messages in the same channel will not result in new notifications
        /// </summary>
        OncePerChannel = 8,
    }
}
