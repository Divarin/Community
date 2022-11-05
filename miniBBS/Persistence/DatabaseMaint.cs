using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Persistence
{
    public static class DatabaseMaint
    {
        public static void Maint(BbsSession session)
        {
            DeleteDataAssociatedWithChannelsThatHaveBeenDeleted(session);
            DeleteDataAssociatedWithUsersThatHaveBeenDeleted(session);
        }

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

        private static void DeleteDataAssociatedWithChannelsThatHaveBeenDeleted(BbsSession session)
        {
            var ucflagRepo = DI.GetRepository<UserChannelFlag>();
            var ucFlags = ucflagRepo.Get();
            var channelIds = DI.GetRepository<Channel>().Get().Select(c => c.Id).ToArray();
            var toBeDeleted = ucFlags?.Where(f => !channelIds.Contains(f.ChannelId))?.ToArray();

            session?.Io.OutputLine($"{toBeDeleted?.Length ?? 0} User/Channel flags found for channels that no longer exist, deleting...");

            if (true == toBeDeleted?.Any())
                ucflagRepo.DeleteRange(toBeDeleted);

            var metaRepo = DI.GetRepository<Metadata>();
            var meta = metaRepo.Get().Where(m => m.ChannelId.HasValue && !channelIds.Contains(m.ChannelId.Value))?.ToArray();

            session?.Io.OutputLine($"{meta?.Length ?? 0} metadata found for channels that no longer exist, deleting...");

            if (true == meta?.Any())
                metaRepo.DeleteRange(meta);
        }

        private static void DeleteDataAssociatedWithUsersThatHaveBeenDeleted(BbsSession session)
        {
            var ucflagRepo = DI.GetRepository<UserChannelFlag>();
            var ucFlags = ucflagRepo.Get();
            var userIds = DI.GetRepository<User>().Get().Select(c => c.Id).ToArray();
            var toBeDeleted = ucFlags?.Where(f => !userIds.Contains(f.UserId))?.ToArray();

            session?.Io.OutputLine($"{toBeDeleted?.Length ?? 0} User/Channel flags found for users that no longer exist, deleting...");

            if (true == toBeDeleted?.Any())
                ucflagRepo.DeleteRange(toBeDeleted);

            var metaRepo = DI.GetRepository<Metadata>();
            var meta = metaRepo.Get().Where(m => m.UserId.HasValue && !userIds.Contains(m.UserId.Value))?.ToArray();

            session?.Io.OutputLine($"{meta?.Length ?? 0} metadata found for users that no longer exist, deleting...");

            if (true == meta?.Any())
                metaRepo.DeleteRange(meta);
        }
    }
}
