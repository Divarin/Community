using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Commands
{
    public static class GopherBookmarks
    {
        public static GopherBookmark GetBookmark(BbsSession session, GopherEntry currentLocation)
        {
            var defaultStart = new GopherBookmark
            {
                Title = "Floodgap",
                Selector = "gopher://gopher.floodgap.com"
            };

            while (true)
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
                {
                    session.Io.OutputLine($"{Constants.Spaceholder}     --- Gopher Bookmarks ---".Color(ConsoleColor.Green));
                    session.Io.OutputLine(
                        $"{Constants.Inverser}1{Constants.Inverser})".Color(ConsoleColor.DarkMagenta) +
                        " Default Start: Floodgap");
                    session.Io.OutputLine(
                        $"{Constants.Inverser}2{Constants.Inverser})".Color(ConsoleColor.DarkMagenta) +
                        " Your Private Bookmarks");
                    session.Io.OutputLine(
                        $"{Constants.Inverser}3{Constants.Inverser})".Color(ConsoleColor.DarkMagenta) +
                        " Public Bookmarks");
                    if (currentLocation != null)
                    {
                        session.Io.OutputLine(
                            $"{Constants.Inverser}4{Constants.Inverser})".Color(ConsoleColor.DarkMagenta) +
                            " Add Bookmark of Current Location");
                    }
                    session.Io.OutputLine(
                        $"{Constants.Inverser}Q{Constants.Inverser})".Color(ConsoleColor.DarkMagenta) +
                        " Quit Bookmark Menu");
                    session.Io.Output($"{Constants.Inverser}Bookmarks{Constants.Inverser}: ".Color(ConsoleColor.Yellow));
                    var key = session.Io.InputKey();
                    session.Io.OutputLine();
                    if (key.HasValue) key = char.ToUpper(key.Value);
                    GopherBookmark bookmark = null;
                    switch (key)
                    {
                        case '1': return defaultStart;
                        case '2':
                            bookmark = GetPrivateBookmark(session);
                            if (bookmark != null) return bookmark;
                            break;
                        case '3':
                            bookmark = GetPublicBookmark(session);
                            if (bookmark != null) return bookmark;
                            break;
                        case '4':
                        case 'A':
                            if (currentLocation != null)
                            {
                                AddBookmark(session, currentLocation);
                            }
                            break;
                        default:
                            return null;
                    }
                }
            }
        }

        private static void AddBookmark(BbsSession session, GopherEntry currentLocation)
        {
            var repo = DI.GetRepository<GopherBookmark>();

            if (currentLocation == null)
            {
                session.Io.Error("Unable to identify current location");
                return;
            }

            if (string.IsNullOrWhiteSpace(currentLocation.Name))
            {
                session.Io.Error("For some reason the current location has no title.");
                session.Io.Output($"{Constants.Inverser}Title{Constants.Inverser}: ".Color(ConsoleColor.Cyan));
                var title = session.Io.InputLine();
                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(title))
                    return;

                currentLocation.Name = title;
            }

            session.Io.OutputLine(
                $"{Constants.Inverser}Bookmarking{Constants.Inverser}: ".Color(ConsoleColor.Yellow) +
                currentLocation.Name);
            session.Io.OutputLine(
                $"{Constants.Inverser}Location{Constants.Inverser}: ".Color(ConsoleColor.Yellow) +
                currentLocation.Url());
            session.Io.OutputLine(
                $"{Constants.Inverser}1{Constants.Inverser}) ".Color(ConsoleColor.DarkMagenta) +
                " Make it Public");
            session.Io.OutputLine(
                $"{Constants.Inverser}2{Constants.Inverser}) ".Color(ConsoleColor.DarkMagenta) +
                " Make it Private");
            session.Io.OutputLine(
                $"{Constants.Inverser}Q{Constants.Inverser})".Color(ConsoleColor.DarkMagenta) +
                "uit Menu");
            session.Io.Output($"{Constants.Inverser}Add Bookmark{Constants.Inverser}: ");
            var key = session.Io.InputKey();
            session.Io.OutputLine();
            if (key.HasValue) key = char.ToUpper(key.Value);

            if (key != '1' && key != '2')
            {
                return;
            }

            var url = currentLocation.Url();
            var priv = key == '2';
            GopherBookmark dupe = null;
            if (priv)
                dupe = repo.Get(new Dictionary<string, object>
                {
                    {nameof(GopherBookmark.Private), true},
                    {nameof(GopherBookmark.UserId), session.User.Id},
                    {nameof(GopherBookmark.Selector), url},
                })?.FirstOrDefault();
            else
                dupe = repo.Get(new Dictionary<string, object>
                {
                    {nameof(GopherBookmark.Private), false},
                    {nameof(GopherBookmark.Selector), url},
                })?.FirstOrDefault();

            if (dupe != null)
            {
                var you = dupe.UserId == session.User.Id;
                session.Io.Error($"{(you ? "You" : "Someone")} {(you ? "have" : "has")} already{(priv ? "" : " publicly")} bookmarked this location.");
                return;
            }

            session.Io.Output($"(optional) {Constants.Inverser}Tags/Keywords{Constants.Inverser}: ");
            var keywords = session.Io.InputLine();
            session.Io.OutputLine();
            keywords = ParseTags(keywords);

            var bookmark = new GopherBookmark
            {
                Title = currentLocation.Name,
                Selector = url,
                UserId = session.User.Id,
                DateCreatedUtc = DateTime.UtcNow,
                Private = priv,
                Tags = keywords,
            };

            repo.Insert(bookmark);
            session.Io.OutputLine("Bookmark added!".Color(ConsoleColor.Green));
        }

        private static GopherBookmark GetPrivateBookmark(BbsSession session)
        {
            var repo = DI.GetRepository<GopherBookmark>();
            var userBms = repo.Get(new Dictionary<string, object>
            {
                {nameof(GopherBookmark.Private), true},
                {nameof(GopherBookmark.UserId), session.User.Id}
            }).ToList();

            return GetBookmark(session, userBms);
        }

        private static GopherBookmark GetPublicBookmark(BbsSession session)
        {
            var repo = DI.GetRepository<GopherBookmark>();
            var publicBms = repo.Get(x => x.Private, false).ToList();

            return GetBookmark(session, publicBms);
        }

        private static GopherBookmark GetBookmark(BbsSession session, List<GopherBookmark> bookmarks)
        {
            var page = 0;
            const int bmsPerPage = 9;
            if (bookmarks == null)
                bookmarks = new List<GopherBookmark>();

            var filteredList = bookmarks;

            while (true)
            {
                var bms = filteredList.Skip(bmsPerPage * page).Take(bmsPerPage).ToList();
                for (var i = 0; i < bmsPerPage && i < bms.Count; i++)
                {
                    var bm = bms[i];
                    session.Io.OutputLine(
                        $"{Constants.Inverser}{i + 1}{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                        $" {bm.Title}");
                }

                session.Io.OutputLine(new string('-', session.Cols - 2).Color(ConsoleColor.Yellow));

                // 1234567890123456789012345678901234567890
                // P)rev. Page   N)ext Page   E)dit/Delete
                // S)earch       #) Go #      Q)uit
                if (filteredList != bookmarks)
                    session.Io.OutputLine($"{Constants.Inverser}Filter(s) Applied{Constants.Inverser}".Color(ConsoleColor.Red));

                session.Io.Output(
                    $"{Constants.Inverser}P{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    $")rev. Page   ");
                session.Io.Output(
                    $"{Constants.Inverser}N{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    $")ext Page   ");
                session.Io.OutputLine(
                    $"{Constants.Inverser}E{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    $")dit/Delete");
                session.Io.Output(
                    $"{Constants.Inverser}S{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    $")earch       ");
                session.Io.Output(
                    $"{Constants.Inverser}#{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    $") Go #      ");
                session.Io.OutputLine(
                    $"{Constants.Inverser}Q{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    $")uit Menu");

                session.Io.Output($"{Constants.Inverser}Bookmarks{Constants.Inverser}: ".Color(ConsoleColor.Yellow));
                var key = session.Io.InputKey();
                session.Io.OutputLine();
                if (!key.HasValue)
                    return null;
                key = char.ToUpper(key.Value);
                switch (key)
                {
                    case 'P':
                        if (page > 0)
                            page--;
                        break;
                    case 'N':
                        if (bms.Count == bmsPerPage)
                            page++;
                        break;
                    case 'E':
                        {
                            var k = session.Io.Ask("Edit which bookmark");
                            var n = k - '1';
                            if (n >= 0 && n < bms.Count)
                            {
                                var edited = EditBookmark(session, bms[n]);
                                if (edited == null)
                                {
                                    // bookmark was deleted
                                    filteredList.Remove(bms[n]);
                                    bookmarks.Remove(bms[n]);
                                }
                            }
                        }
                        break;
                    case 'S':
                        filteredList = ApplySearch(session, filteredList);
                        if (filteredList == null)
                            filteredList = bookmarks; // clear all filters
                        break;
                    case 'Q':
                        return null;
                    default:
                        {
                            var n = key.Value - '1';
                            if (n >= 0 && n < bms.Count)
                            {
                                return bms[n];
                            }
                        }
                        break;
                }
            }
        }

        private static List<GopherBookmark> ApplySearch(BbsSession session, List<GopherBookmark> bookmarks)
        {
            session.Io.OutputLine(
                $"{Constants.Inverser}C{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                ")lear all filters");
            session.Io.Output($"{Constants.Inverser}Search Term{Constants.Inverser}: ".Color(ConsoleColor.Yellow));
            var search = session.Io.InputLine();
            session.Io.OutputLine();
            if (string.IsNullOrWhiteSpace(search))
                return bookmarks;
            search = search.Trim().ToLower();

            if (search == "c")
                return null; // signify clear all filters

            var query = from bm in bookmarks
                          let tags = bm.Tags.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLower())
                          where bm.Title.ToLower().Contains(search) || tags.Contains(search)
                          select bm;

            var results = query.ToList();
            if (results.Count > 0)
                return results;
            session.Io.Error("No results");
            return bookmarks;
        }

        private static GopherBookmark EditBookmark(BbsSession session, GopherBookmark bookmark)
        {
            var canEdit =
                session.User.Access.HasFlag(AccessFlag.Administrator) ||
                session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                session.User.Id == bookmark.UserId;

            if (!canEdit)
            {
                session.Io.Error("Can't edit someone else's bookmark");
                return bookmark;
            }

            var repo = DI.GetRepository<GopherBookmark>();

            while (true)
            {
                session.Io.OutputLine(
                    $"{Constants.Inverser}T{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    ")itle: " + bookmark.Title.Color(ConsoleColor.DarkCyan));
                session.Io.OutputLine(
                    $"{Constants.Inverser}L{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    ")ocation: " + bookmark.Selector.Color(ConsoleColor.DarkCyan));
                session.Io.OutputLine(
                    $"{Constants.Inverser}P{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    ")ublic/Private: " + (bookmark.Private ? "Private".Color(ConsoleColor.Red) : "Public".Color(ConsoleColor.Green)));
                session.Io.OutputLine(
                    $"{Constants.Inverser}A{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    ")dd Keyword(s)");
                session.Io.OutputLine(
                    $"{Constants.Inverser}K{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    ")eywords (edit): " + bookmark.Tags.Color(ConsoleColor.DarkCyan));
                session.Io.OutputLine(
                    $"{Constants.Inverser}D{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    ")elete");
                session.Io.OutputLine(
                    $"{Constants.Inverser}Q{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    ")uit Menu");
                session.Io.Output($"{Constants.Inverser}Edit Bookmark{Constants.Inverser}: ".Color(ConsoleColor.Yellow));
                var key = session.Io.InputKey();
                session.Io.OutputLine();
                if (key.HasValue) key = char.ToUpper(key.Value);
                switch (key)
                {
                    case 'T':
                        {
                            session.Io.Output($"{Constants.Inverser}New Title{Constants.Inverser}: ".Color(ConsoleColor.Magenta));
                            var title = session.Io.InputLine();
                            session.Io.OutputLine();
                            if (!string.IsNullOrWhiteSpace(title) && !title.Equals(bookmark.Title))
                            {
                                bookmark.Title = title;
                                repo.Update(bookmark);
                            }
                        }
                        break;
                    case 'L':
                        {
                            session.Io.Output($"{Constants.Inverser}New Location{Constants.Inverser}: ".Color(ConsoleColor.Magenta));
                            var selector = session.Io.InputLine();
                            session.Io.OutputLine();
                            if (!string.IsNullOrWhiteSpace(selector) && !selector.Equals(bookmark.Selector))
                            {
                                bookmark.Selector = selector;
                                repo.Update(bookmark);
                            }
                        }
                        break;
                    case 'P':
                        bookmark.Private = !bookmark.Private;
                        repo.Update(bookmark);
                        break;
                    case 'A':
                    case 'K':
                        {
                            var add = key == 'A';
                            session.Io.Output($"{Constants.Inverser}{(add ? "Add" : "Edit")} Keyword(s){Constants.Inverser}: ".Color(ConsoleColor.Magenta));
                            var keywords = session.Io.InputLine();
                            session.Io.OutputLine();
                            if (!add && string.IsNullOrWhiteSpace(keywords) && 'Y' != session.Io.Ask("Remove all keywords?"))
                                break;
                            keywords = add ? ParseTags(bookmark.Tags, keywords) : ParseTags(keywords);
                            if (!keywords.Equals(bookmark.Tags))
                            {
                                bookmark.Tags = keywords;
                                repo.Update(bookmark);
                            }
                        }
                        break;
                    case 'D':
                        if ('Y' == session.Io.Ask("Delete this bookmark?"))
                        {
                            repo.Delete(bookmark);
                            return null;
                        }
                        break;
                    default:
                        return bookmark;
                }
            }
        }

        private static string ParseTags(params string[] wordSets)
        {
            if (wordSets == null || wordSets.Length < 1)
                return string.Empty;

            var tags = new HashSet<string>(from wordSet in wordSets
                                           from word in wordSet.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                           orderby word
                                           select word);
            return string.Join(" ", tags);
        }

    }
}
