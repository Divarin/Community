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
            if (session?.User == null || di == null)
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
            if (session?.User == null)
                return null;

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

            if (meta != null && meta.TryGetDataAs<HashSet<int>>(out HashSet<int> set))
            {
                session.Items[SessionItem.ReadMessages] = set;
                return set;
            }

            return null;
        }

        private static Metadata GetReadsMetaFromDatabase(BbsSession session, IRepository<Metadata> metaRepo)
        {
            if (session?.User == null)
                return null;

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

        /// <summary>
        /// "1-5,7,9,11-13" => 1,2,3,4,5,7,9,11,12,13
        /// </summary>
        //private static HashSet<int> Deconsolidate(string ranges)
        //{
        //    var result = new HashSet<int>();
        //    if (string.IsNullOrWhiteSpace(ranges))
        //        return result;

        //    if (ranges.StartsWith("["))
        //        ranges = ranges.Substring(1);
        //    if (ranges.EndsWith("]"))
        //        ranges = ranges.Substring(0, ranges.Length - 1);
            
        //    var segments = ranges.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        //    foreach (var segment in segments)
        //    {
        //        if (int.TryParse(segment, out int n))
        //            result.Add(n);
        //        else
        //        {
        //            var pair = segment.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
        //            if (pair.Length == 2 && int.TryParse(pair[0], out int n1) && int.TryParse(pair[1], out int n2))
        //            {
        //                for (int i = n1; i <= n2; i++)
        //                    result.Add(i);
        //            }
        //        }
        //    }

        //    return result;
        //}

        /// <summary>
        /// 1,2,3,4,5,7,9,11,12,13 => "1-5,7,9,11-13"
        /// </summary>
        //private static string Consolidate(HashSet<int> set)
        //{
        //    var min = set.Min();
        //    var max = set.Max();

        //    int? rangeLast = null;
        //    int? rangeStart = null;
        //    int? rangeEnd = null;

        //    var builder = new StringBuilder();

        //    for (int i=min; i < max; i++)
        //    {
        //        if (!set.Contains(i)) 
        //            continue;
        //        if (!rangeLast.HasValue)
        //            rangeStart = rangeLast = i;
        //        else if (i == rangeLast.Value + 1)
        //            rangeEnd = rangeLast = i;
        //        else
        //        {
        //            // end of range (or single)
        //            if (rangeEnd.HasValue) // range
        //                builder.Append($"{rangeStart}-{rangeEnd}");
        //            else // single
        //                builder.Append($"{rangeStart}");
        //            rangeEnd = null;

        //            rangeStart = rangeLast = i;

        //            builder.Append(",");
        //        }
        //    }

        //    builder.Append(max.ToString());

        //    return builder.ToString();
        //}
    }
}
