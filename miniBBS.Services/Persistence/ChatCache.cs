using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Data;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Services.Persistence
{
    public class ChatCache : IChatCache
    {
        private readonly IRepository<Chat> _chatRepo;
        // [Channel ID][Chat ID] = Chat message.
        private readonly ConcurrentDictionary<int, SortedList<int, Chat>> _channelChats = new ConcurrentDictionary<int, SortedList<int, Chat>>();

        public ChatCache()
        {
            _chatRepo = GlobalDependencyResolver.Default.GetRepository<Chat>();
        }

        public void Clear()
        {
            _channelChats.Clear();
        }

        public SortedList<int, Chat> GetChannelChats(int channelId)
        {
            if (!_channelChats.ContainsKey(channelId))
                _channelChats[channelId] = GetChannelChatsFromRepo(channelId);

            return _channelChats[channelId];
        }

        public void UpdateChat(Chat chat)
        {
            var channelChats = GetChannelChats(chat.ChannelId);
            if (channelChats.ContainsKey(chat.Id))
            {
                channelChats[chat.Id] = chat;
            }
        }

        public void DeleteChat(Chat chat)
        {
            var channelChats = GetChannelChats(chat.ChannelId);
            if (channelChats.ContainsKey(chat.Id))
            {
                channelChats.Remove(chat.Id);
            }
        }

        private SortedList<int, Chat> GetChannelChatsFromRepo(int channelId)
        {
            return new SortedList<int, Chat>(_chatRepo
                .Get(c => c.ChannelId, channelId)
                .ToDictionary(k => k.Id));
        }

    }
}
