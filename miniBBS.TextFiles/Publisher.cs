using miniBBS.Core.Models.Control;
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
