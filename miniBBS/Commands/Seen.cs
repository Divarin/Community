using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions_Model;
using miniBBS.Extensions_UserIo;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace miniBBS.Commands
{
    public static class Seen
    {
        public static void Execute(BbsSession session, params string[] args)
        {
            var repo = DI.GetRepository<Metadata>();
            if (true != args?.Any())
                ShowLastSeenUser(session, repo);
            else
                ShowUsers(session, repo, args);
        }

        private static void ShowLastSeenUser(BbsSession session, IRepository<Metadata> repo)
        {
            var last = repo.Get(x => x.Type, MetadataType.SeenData)
                ?.Where(x => x.DateAddedUtc.HasValue)
                ?.OrderByDescending(x => x.DateAddedUtc.Value)
                ?.FirstOrDefault();

            if (last?.UserId != null)
            {
                var data = JsonConvert.DeserializeObject<SeenData>(last.Data);
                data.Show(session, last.UserId.Value);
            }
            else
                session.Io.Error("I don't remember the last time someone was on here.");
        }

        private static void ShowUsers(BbsSession session, IRepository<Metadata> repo, string[] usernames)
        {
            var userIds = session.Usernames
                .Where(x => usernames.Contains(x.Value, StringComparer.CurrentCultureIgnoreCase))
                .Select(x => x.Key)
                .ToArray();

            var reverseDict = session.Usernames
                .ToDictionary(k => k.Value, v => v.Key, StringComparer.CurrentCultureIgnoreCase);

            var seens = repo.Get(x => x.Type, MetadataType.SeenData)
                ?.Where(x => x.UserId.HasValue && userIds.Contains(x.UserId.Value))
                ?.ToDictionary(k => k.UserId);

            foreach (var un in usernames)
            {
                if (reverseDict.TryGetValue(un, out int userId) && seens.ContainsKey(userId))
                {
                    var seen = seens[userId];
                    var data = JsonConvert.DeserializeObject<SeenData>(seen.Data);
                    data.Show(session, seen.UserId.Value);
                }
                else
                    session.Io.Error($"I don't remember the last time {un} was on here.");
            }
        }
    }
}
