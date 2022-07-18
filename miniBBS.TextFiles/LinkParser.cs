using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.TextFiles.Extensions;
using miniBBS.TextFiles.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace miniBBS.TextFiles
{
    public static class LinkParser
    {
        private const string _anchorTag = "<A HREF=\"";
        public static IEnumerable<Link> GetLinks(string body)
        {
            int pos = 0;
            do
            {
                pos = body.IndexOf(_anchorTag, pos, StringComparison.CurrentCultureIgnoreCase);
                if (pos < 0)
                    break;
                pos += _anchorTag.Length;
                int end = body.IndexOf("\">", pos);
                int len = end - pos;
                var link = new Link
                {
                    ActualFilename = body.Substring(pos, len).Replace("&amp", "&").Replace(";", "")
                };
                pos = body.IndexOf("\">", pos) + 2;
                end = body.IndexOf("</A>", pos, StringComparison.CurrentCultureIgnoreCase);
                len = end - pos;
                link.DisplayedFilename = body.Substring(pos, len);
                pos = end;
                end = body.IndexOf("\n", pos);
                if (end <= pos)
                    continue;
                len = end - pos;
                var desc = ExtractDescription(body.Substring(pos, len));
                if (desc == null)
                    continue;
                link.Description = desc;
                if (link.ActualFilename.EndsWith(".gz", StringComparison.CurrentCultureIgnoreCase) || link.ActualFilename.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase))
                    continue;
                pos = end; // skip to end of line just in-case there's other links in this line
                yield return link;
            } while (true);
        }

        public static IList<Link> GetLinksFromIndex(BbsSession session, Link indexLocation)
        {
            string dir = Constants.TextFileRootDirectory;
            if (indexLocation.Parent != null)
                dir += indexLocation.Parent.Path;

            dir = JoinPathParts(dir, indexLocation.ActualFilename);

            List<Link> links;
            if (File.Exists(dir))
            {
                var txt = FileReader.ReadFile(new FileInfo(dir));
                links = GetLinks(txt).ToList();
                if (indexLocation.IsOwnedByUser(session.User))
                {
                    var unpublished = GetUnindexedLinks(dir, indexLocation)
                        ?.Where(l => !l.IsDirectory && !links.Any(x => x.DisplayedFilename.Equals(l.DisplayedFilename)))
                        ?.Select(l =>
                        {
                            l.Description = "(UNPUBLISHED)";
                            return l;
                        });
                    if (true == unpublished?.Any())
                        links.AddRange(unpublished);
                }
            }
            else
                links = GetUnindexedLinks(dir, indexLocation);

            foreach (var link in links)
                link.Parent = indexLocation;
            return links;
        }

        private static List<Link> GetUnindexedLinks(string dir, Link indexLocation)
        {
            List<Link> links = new List<Link>();
            dir = dir
                .ToLower()
                .Replace("index.html", "");

            DirectoryInfo di = new DirectoryInfo(dir);
            foreach (DirectoryInfo subdir in di.GetDirectories())
                links.Add(new Link
                {
                    DisplayedFilename = subdir.Name,
                    ActualFilename = subdir.Name + "/index.html",
                    Parent = indexLocation,
                    Description = string.Empty
                });

            foreach (FileInfo file in di.GetFiles())
                links.Add(new Link
                {
                    DisplayedFilename = file.Name,
                    ActualFilename = file.Name,
                    Parent = indexLocation,
                    Description = string.Empty
                });

            return links;
        }

        private static string JoinPathParts(params string[] pathParts)
        {
            pathParts = pathParts
                .Select(p =>
                {
                    if (p.StartsWith("\\") || p.StartsWith("/"))
                        p = p.Substring(1);
                    if (p.EndsWith("\\") || p.EndsWith("/"))
                        p = p.Substring(0, p.Length - 1);
                    return p;
                })
                .ToArray();

            string path = string.Join("\\", pathParts).Replace("/", "\\");

            return path;
        }

        private static string ExtractDescription(string text)
        {
            // find first <B> tag, strip off anything before that.
            int pos = text.IndexOf("<B>", StringComparison.CurrentCultureIgnoreCase);
            if (pos <= 0)
                pos = text.IndexOf("<BR>", StringComparison.CurrentCultureIgnoreCase);
            if (pos <= 0)
                return null;

            text = text.Substring(pos);
            // replace newline tags with newline characters
            text = text.Replace("<BR>", Environment.NewLine);
            // replace html spaces with actual spaces
            text = text.Replace("&nbsp", " ");
            
            // remove any remaining tags
            var builder = new List<char>();
            bool inTag = false;
            foreach (var c in text)
            {
                if (!inTag && c == '<')
                    inTag = true;
                else if (inTag && c == '>')
                    inTag = false;
                else if (!inTag)
                    builder.Add(c);
            }
            string result = new string(builder.ToArray());

            // trim any whitespace characters from start/end
            result = result.Trim();

            return result;
        }
    }
}
