using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
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

        public static void LoadChatHeaderFormat(this BbsSession session, IRepository<Metadata> metaRepo)
        {
            var meta = metaRepo.Get(new Dictionary<string, object>
                {
                    {nameof(Metadata.UserId), session.User.Id},
                    {nameof(Metadata.Type), MetadataType.ChatHeaderFormat}
                })?.PruneAllButMostRecent(metaRepo);

            string headerFormat = Constants.DefaultChatHeaderFormat;
            
            if (!string.IsNullOrWhiteSpace(meta?.Data))
                headerFormat = meta.Data;

            session.Items[SessionItem.ChatHeaderFormat] = headerFormat;
        }

        public static CrossChannelNotificationMode GetCrossChannelNotificationMode(this BbsSession session, IRepository<Metadata> metaRepo)
        {
            var xchanmode = Constants.DefaultCrossChannelNotificationMode;

            if (!session.Items.ContainsKey(SessionItem.CrossChannelNotificationMode))
            {
                var meta = metaRepo.Get(new Dictionary<string, object>
                {
                    {nameof(Metadata.UserId), session.User.Id},
                    {nameof(Metadata.Type), MetadataType.CrossChannelNotifications}
                })?.PruneAllButMostRecent(metaRepo);
                if (meta == null)
                {
                    meta = new Metadata
                    {
                        UserId = session.User.Id,
                        Data = xchanmode.ToString(),
                        DateAddedUtc = DateTime.UtcNow,
                        Type = MetadataType.CrossChannelNotifications
                    };
                    metaRepo.Insert(meta);
                }
                if (Enum.TryParse(meta.Data, out CrossChannelNotificationMode x))
                    xchanmode = x;
                session.Items[SessionItem.CrossChannelNotificationMode] = xchanmode;
            }

            return (CrossChannelNotificationMode)session.Items[SessionItem.CrossChannelNotificationMode];
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
