using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Data;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Persistence
{
    public static class DatabaseMaint
    {
        public static void RemoveSuperfluousUserChannelFlags(IRepository<UserChannelFlag> repo, int userId)
        {
            var recs = repo.Get(x => x.UserId, userId);
            if (true != recs?.Any())
                return;

            var group = recs
                .GroupBy(r => r.ChannelId)
                .ToDictionary(k => k.Key, v => v.ToList());

            List<UserChannelFlag> toBeDeleted = new List<UserChannelFlag>();
            foreach (var chan in group)
            {
                var list = chan.Value;
                var max = list.Max(x => x.LastReadMessageNumber);
                var maxFlag = list.First(x => x.LastReadMessageNumber == max);
                toBeDeleted.AddRange(list.Where(x => x != maxFlag));
            }

            repo.DeleteRange(toBeDeleted);
        }
    }
}
