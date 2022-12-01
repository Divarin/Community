using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions_Model;
using miniBBS.Extensions_Repo;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Extensions_ReadTracker
{
    public static class ReadMessageTracker
    {
        public static List<int> ReadChatIds(this BbsSession session, IDependencyResolver di)
        {
            if (session == null || di == null)
                return new List<int>();

            var set = GetReads(session, di);
            return set.ToList(); // copy so that the collection can't be manipulated outside of these extension methods.
        }

        public static bool HasRead(this BbsSession session, int chatId, IDependencyResolver di)
        {
            if (session == null || di == null)
                return false;

            var set = GetReads(session, di);
            var hasRead = set.Contains(chatId);
            return hasRead;
        }

        public static void MarkRead(this BbsSession session, int chatId, IDependencyResolver di, bool asRead = true)
        {
            if (session == null || di == null)
                return;

            var set = GetReads(session, di);
            if (asRead)
                set.Add(chatId);
            else if (set.Contains(chatId))
                set.Remove(chatId);
        }

        public static void SaveReads(this BbsSession session, IDependencyResolver di)        
        {
            if (session == null || di == null)
                return;

            var set = GetReads(session, di);
            var metaRepo = di.GetRepository<Metadata>();
            var meta = GetReadsMetaFromDatabase(session, metaRepo);
            if (meta == null)
                meta = new Metadata
                {
                    Type = MetadataType.ReadMessages,
                    UserId = session.User.Id,
                    DateAddedUtc = DateTime.UtcNow
                };

            meta.Data = JsonConvert.SerializeObject(set);
            metaRepo.InsertOrUpdate(meta);
        }

        private static HashSet<int> GetReads(BbsSession session, IDependencyResolver di)
        {
            if (session == null || di == null)
                return new HashSet<int>();

            HashSet<int> set =
                TryGetReadsSetFromSession(session.Items) ??
                TryGetReadsSetFromAnotherSession(session, di) ??
                TryGetReadsSetFromDatabase(session, di) ??
                CreateNewReadsSet(session);

            return set;
        }

        private static HashSet<int> TryGetReadsSetFromSession(IDictionary<SessionItem, object> sessionItems)
        {
            if (true == sessionItems?.ContainsKey(SessionItem.ReadMessages) && sessionItems[SessionItem.ReadMessages] is HashSet<int> set)
                return set;
            return null;
        }

        private static HashSet<int> TryGetReadsSetFromAnotherSession(BbsSession session, IDependencyResolver di)
        {
            var sessionsList = di.Get<ISessionsList>();
            var otherSesssions = sessionsList.Sessions.Where(s => s.User?.Id == session.User.Id && s.Id != session.Id).ToList();
            if (true != otherSesssions?.Any())
                return null;
            foreach (var s in otherSesssions)
            {
                var set = TryGetReadsSetFromSession(s.Items);
                if (set != null)
                    return set;
            }
            return null;
        }

        private static HashSet<int> TryGetReadsSetFromDatabase(BbsSession session, IDependencyResolver di)
        {
            Metadata meta = GetReadsMetaFromDatabase(session, di.GetRepository<Metadata>());

            if (meta != null && meta.TryGetDataAs(out HashSet<int> set))
            {
                session.Items[SessionItem.ReadMessages] = set;
                return set;
            }

            return null;
        }

        private static Metadata GetReadsMetaFromDatabase(BbsSession session, IRepository<Metadata> metaRepo)
        {
            var meta = metaRepo.Get(new Dictionary<string, object>
            {
                {nameof(Metadata.Type), MetadataType.ReadMessages},
                {nameof(Metadata.UserId), session.User.Id}
            })?.PruneAllButMostRecent(metaRepo);
            return meta;
        }

        private static HashSet<int> CreateNewReadsSet(BbsSession session)
        {
            var set = new HashSet<int>();
            session.Items[SessionItem.ReadMessages] = set;
            return set;
        }
    }
}
