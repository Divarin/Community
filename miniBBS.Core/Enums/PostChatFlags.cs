using System;

namespace miniBBS.Core.Enums
{
    [Flags]
    public enum PostChatFlags
    {
        None = 0,
        IsNewTopic = 1,
        IsWebVisible = 2,
        IsWebInvisible = 4
    }
}
