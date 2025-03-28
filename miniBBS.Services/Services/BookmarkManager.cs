using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Services
{
    public static class BookmarkManager
    {
        public static BookmarkedRead GetBookmarkedRead(BbsSession session)
        {
            var repo = GlobalDependencyResolver.Default.GetRepository<Metadata>();
            var metadata = repo.Get(new Dictionary<string, object>
            {
                {nameof(Metadata.UserId), session.User.Id},
                {nameof(Metadata.Type), MetadataType.Bookmark}
            });
            if (metadata?.Any() != true)
            {
                return null;
            }
            var bookmarkedRead = Decompress(metadata.First().Data);
            return bookmarkedRead;
        }

        public static void DeleteBookmark(BbsSession session)
        {
            var repo = GlobalDependencyResolver.Default.GetRepository<Metadata>();
            var metadata = repo.Get(new Dictionary<string, object>
            {
                {nameof(Metadata.UserId), session.User.Id},
                {nameof(Metadata.Type), MetadataType.Bookmark}
            });
            if (metadata?.Any() == true)
            {
                repo.DeleteRange(metadata);
            }
        }

        public static void SaveBookmark(BbsSession session, string text, double percentage, OutputHandlingFlag handlingFlags)
        {
            var bmr = new BookmarkedRead
            {
                FullText = text,
                Percentage = percentage,
                OutputFlags = handlingFlags,
                TextColor = session.Io.GetForeground(),
            };
            var metaRepo = GlobalDependencyResolver.Default.GetRepository<Metadata>();
            var metadata = new Metadata
            {
                UserId = session.User.Id,
                Type = MetadataType.Bookmark,
                Data = Compress(bmr),
                DateAddedUtc = DateTime.UtcNow,
            };
            // remove this user's old bookmarks, if any
            var old = metaRepo.Get(new Dictionary<string, object>
            {
                {nameof(Metadata.UserId), session.User.Id},
                {nameof(Metadata.Type), MetadataType.Bookmark}
            });
            if (old?.Any() == true)
            {
                metaRepo.DeleteRange(old);
            }
            // add new
            metaRepo.Insert(metadata);
        }

        public static void Read(BbsSession session, BookmarkedRead bmr)
        {
            var originalDnd = session.DoNotDisturb;
            session.DoNotDisturb = true;

            try
            {
                session.Items[SessionItem.BookmarkPercentage] = bmr.Percentage;
                var flags = bmr.OutputFlags;
                flags |= OutputHandlingFlag.AdvanceToPercentage;
                flags |= OutputHandlingFlag.PauseAtEnd;
                using (session.Io.WithColorspace(ConsoleColor.Black, bmr.TextColor))
                {
                    session.Io.OutputLine(bmr.FullText, flags);
                }
            }
            finally
            {
                session.DoNotDisturb = originalDnd;
            }
        }

        public static bool CheckBookmarkedRead(BbsSession session)
        {
            var bmr = GetBookmarkedRead(session);
            if (bmr == null)
                return false;
            if ('Y' == session.Io.Ask("You have a bookmark, continue reading?".Color(ConsoleColor.Red)))
                Read(session, bmr);
            if ('Y' == session.Io.Ask("Delete bookmark?".Color(ConsoleColor.Red)))
                DeleteBookmark(session);
            return true;
        }

        private static string Compress(BookmarkedRead bmr)
        {
            var uncompressed = JsonConvert.SerializeObject(bmr);
            var compressor = GlobalDependencyResolver.Default.Get<ICompressor>();
            var compressed = compressor.Compress(uncompressed);
            return compressed;
        }

        private static BookmarkedRead Decompress(string compressed)
        {
            var compressor = GlobalDependencyResolver.Default.Get<ICompressor>();
            var json = compressor.Decompress(compressed);
            var bmr = JsonConvert.DeserializeObject<BookmarkedRead>(json);
            return bmr;
        }
    }
}
