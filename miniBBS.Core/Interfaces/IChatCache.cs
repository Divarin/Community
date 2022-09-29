using miniBBS.Core.Models.Data;
using System.Collections.Generic;

namespace miniBBS.Services.Persistence
{
    public interface IChatCache
    {
        SortedList<int, Chat> GetChannelChats(int channelId);
    }
}
