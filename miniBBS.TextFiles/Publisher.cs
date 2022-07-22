using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using miniBBS.TextFiles.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.TextFiles
{
    public static class Publisher
    {
        public static void Publish(BbsSession session, Link currentLocation, string filenameOrNumber, IList<Link> links)
        {
            Link file = FindFile(links, filenameOrNumber);
            if (file == null)
            {
                session.Io.OutputLine("Invalid filename name");
                return;
            }
            
            session.Io.Output($"Enter description{(string.IsNullOrEmpty(file.Description) ? "" : " Enter=Abort")}: ");
            string descr = session.Io.InputLine();
            if (!string.IsNullOrWhiteSpace(descr))
            {
                file.Description = descr;
                IndexUpdater.UpdateIndex(currentLocation, file);
                session.Io.OutputLine($"{file.DisplayedFilename} is now visible to other users!");
            }
        }

        public static void Unpublish(BbsSession session, Link currentLocation, string filenameOrNumber, IList<Link> links)
        {
            Link file = FindFile(links, filenameOrNumber);
            if (file == null)
            {
                session.Io.OutputLine("Invalid filename name");
                return;
            }

            session.Io.Output($"Are you sure you want to unpublish {file.DisplayedFilename}?: ");
            var k = session.Io.InputKey();
            session.Io.OutputLine();
            if (k == 'Y' || k == 'y')
            {
                IndexUpdater.UpdateIndex(currentLocation, file, delete: true);
                session.Io.OutputLine($"{file.DisplayedFilename} is no longer visible to other users!");
            }
        }

        public static void SetEditor(BbsSession session, Link currentLocation, string filenameOrNumber, string username, bool add, IList<Link> links)
        {
            Link file = FindFile(links, filenameOrNumber);
            if (file == null)
            {
                session.Io.OutputLine("Invalid filename name");
                return;
            }

            session.Io.Output($"Are you sure you want to {(add ? "add" : "remove")} {username} as an editor/contributor to {file.DisplayedFilename}?: ");
            var k = session.Io.InputKey();
            session.Io.OutputLine();
            if (k == 'Y' || k == 'y')
            {
                if (add && true != file.Editors?.Contains(username, StringComparer.CurrentCultureIgnoreCase))
                {
                    // contrib
                    if (true == file.Editors?.Contains("*"))
                    {
                        // since everyone can already edit, is this user currently blacklisted? if so, remove that blacklist
                        var blacklist = file.Editors.FirstOrDefault(x => $"-{username}".Equals(x, StringComparison.CurrentCultureIgnoreCase));
                        if (!string.IsNullOrWhiteSpace(blacklist))
                            file.Editors = file.Editors.Where(x => !blacklist.Equals(x, StringComparison.CurrentCultureIgnoreCase)).ToArray();
                        // otherwise, no need to add this user specifically because the "*" will let them edit
                    }
                    else
                    {
                        var arr = new[] { username };
                        if (file.Editors == null)
                            file.Editors = arr;
                        else if (username == "*")
                            file.Editors = new[] { "*" }.Union(file.Editors.Where(x => x.StartsWith("-"))).ToArray(); // allow all except blacklisted
                        else
                            file.Editors = file.Editors.Union(arr, StringComparer.CurrentCultureIgnoreCase).ToArray();
                    }
                }
                else if (!add)
                {
                    // uncontrib
                    file.Editors = file.Editors
                        ?.Where(x => !username.Equals(x, StringComparison.CurrentCultureIgnoreCase))
                        ?.ToArray();

                    if (file.Editors?.Count == 0 || username == "*")
                        file.Editors = null;
                    else if (file.Editors.Contains("*") && !file.Editors.Contains($"-{username}", StringComparer.CurrentCultureIgnoreCase))
                    {
                        // since "all" are allowed to edit, and we're "uncontrib'ing" this user, then blacklist the user
                        file.Editors = file.Editors.Union(new[] { $"-{username}" }).ToArray();
                    }
                }
                IndexUpdater.UpdateIndex(currentLocation, file, delete: true);
                IndexUpdater.UpdateIndex(currentLocation, file, delete: false);
                session.Io.OutputLine("Done.");
            }
        }

        private static Link FindFile(IList<Link> links, string filenameOrNumber)
        {
            Link file = null;
            if (int.TryParse(filenameOrNumber, out int n) && n >= 1 && n <= links.Count)
                file = links[n - 1];
            else
                file = links.FirstOrDefault(l => l.DisplayedFilename.Equals(filenameOrNumber, StringComparison.CurrentCultureIgnoreCase));
            return file;
        }
    }
}
