using miniBBS.Core.Enums;
using miniBBS.Core.Helpers;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Persistence
{
    public static class DatabaseMaint
    {
        public static void Maint(BbsSession session)
        {
            DeleteSlackers(session);
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

        private static void DeleteSlackers(BbsSession session)
        {
            var now = DateTime.UtcNow;

            var all = session.UserRepo.Get();

            if (all?.Any() != true)
                return;

            var l1Slackers = all
                .Where(x => x.TotalLogons == 1)
                .Where(x => (now - x.LastLogonUtc).TotalDays > 30)
                .ToList();

            var l2Slackers = all
                .Where(x => x.TotalLogons > 1 && x.TotalLogons < 5)
                .Where(x => (now - x.LastLogonUtc).TotalDays > 90)
                .ToList();

            var l3Slackers = all
                .Where(x => (now - x.LastLogonUtc).TotalDays > 366)
                .ToList();

            var slackers = new HashSet<User>(new LambdaComparer<User, int>(u => u.Id));
            foreach (var slacker in l1Slackers)
                slackers.Add(slacker);
            foreach (var slacker in l2Slackers)
                slackers.Add(slacker);
            foreach (var slacker in l3Slackers)
                slackers.Add(slacker);

            if (slackers?.Any() != true)
                return;

            var userIds = slackers.Select(x => x.Id).ToArray();
            var chatUserIds = DI.GetRepository<Chat>().GetDistinct(x => x.FromUserId);
            var filteredSlackers = slackers.Where(x => !chatUserIds.Contains(x.Id)).ToList();
            var bulletinUserIds = DI.GetRepository<Bulletin>().GetDistinct(x => x.FromUserId);
            filteredSlackers = filteredSlackers.Where(x => !bulletinUserIds.Contains(x.Id)).ToList();

            if (filteredSlackers?.Any() != true)
                return;

            var exitMenu = false;
            while (!exitMenu)
            {
                session.Io.OutputLine($"There are {filteredSlackers.Count} slackers to be deleted.");
                session.Io.Output("(S)kip, (R)eview, (P)roceed: ");
                var key = session.Io.InputKey();
                session.Io.OutputLine();
                if (!key.HasValue)
                    return;
                key = char.ToUpper(key.Value);
                switch (key.Value)
                {
                    case 'R':
                        session.Io.OutputLine(string.Join(session.Io.NewLine, filteredSlackers
                            .OrderBy(x => x.Name)
                            .Select(s => $"{s.Name} ({s.TotalLogons})\t{s.DateAddedUtc}\t{s.LastLogonUtc}")), OutputHandlingFlag.PauseAtEnd);
                        break;
                    case 'P':
                        session.UserRepo.DeleteRange(filteredSlackers);
                        exitMenu = true;
                        break;
                    default:
                        exitMenu = true;
                        break;
                }
            }
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
