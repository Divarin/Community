namespace miniBBS.Core.Enums
{
    public enum SessionItem
    {
        LogoutMessage,
        LastWhisperFromUserId,
        IgnoreList,
        ShownTutorMessages,
        ReadMessages,
        ChatHeaderFormat,
        CrossChannelNotificationMode,
        CrossChannelNotificationReceivedChannels,
        /// <summary>
        /// Has a lock on a basic program)
        /// </summary>
        BasicLock,
        /// <summary>
        /// Color of the key being held (if user is in nullspace)
        /// </summary>
        NullspaceKey,
        /// <summary>
        /// While logging off from main menu, we skip showing things that happened in chat while the user was in Do Not Disturb mode
        /// </summary>
        DoNotShowDndSummary
    }
}
