using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Extensions
{
    public static class SessionExtensions
    {
        public static HashSet<string> IgnoreList(this BbsSession session)
        {
            if (true == session?.Items?.ContainsKey(SessionItem.IgnoreList))
                return session.Items[SessionItem.IgnoreList] as HashSet<string>;
            return null;
        }

        public static bool IsIgnoring(this BbsSession session, string username)
        {
            return true == session?.IgnoreList()?.Contains(username, StringComparer.CurrentCultureIgnoreCase);
        }

        public static bool IsIgnoring(this BbsSession session, int userId)
        {
            var username = session.Username(userId);
            return username != null && session.IsIgnoring(username);
        }

        public static void RecordSeenData(this BbsSession session, IRepository<Metadata> metaRepo)
        {
            if (session?.User == null)
                return;

            var data = new SeenData
            {
                SessionsStartUtc = session.SessionStartUtc,
                SessionEndUtc = DateTime.UtcNow,
                QuitMessage = session.Items.ContainsKey(SessionItem.LogoutMessage) ? 
                    session.Items[SessionItem.LogoutMessage] as string ?? string.Empty : 
                    string.Empty
            };

            var meta = new Metadata
            {
                UserId = session.User.Id,
                Type = MetadataType.SeenData,
                Data = JsonConvert.SerializeObject(data)
            };

            var oldMetas = metaRepo.Get(new Dictionary<string, object>
            {
                {nameof(Metadata.UserId), meta.UserId},
                {nameof(Metadata.Type), meta.Type}
            })?.ToList();

            if (true == oldMetas?.Any())
                metaRepo.DeleteRange(oldMetas);

            metaRepo.Insert(meta);
        }

        public static string Username(this BbsSession session, int userId)
        {
            return session?.Usernames?.GetOrDefault(userId, "Unknown") ?? "Unknown";
        }
    }
}
