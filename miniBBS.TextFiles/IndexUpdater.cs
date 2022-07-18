using miniBBS.Core;
using miniBBS.TextFiles.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace miniBBS.TextFiles
{
    public class IndexUpdater
    {
        public static void UpdateIndex(Link indexLocation, Link item, bool delete = false)
        {
            var data = FileReader.LoadFileContents(indexLocation, new Link
            {
                ActualFilename = "index.html",
                Parent = indexLocation
            });

            var linkComparer = new LinkComparer();

            var links = LinkParser.GetLinks(data);
            var dirs = links.Where(l => l.IsDirectory).ToList();
            dirs.Sort(linkComparer);
            var files = links.Where(l => !l.IsDirectory).ToList();
            files.Sort(linkComparer);

            var listToInsertInto = item.IsDirectory ? dirs : files;

            var itemIndex = listToInsertInto.BinarySearch(item, linkComparer);
            if (!delete)
            {
                if (itemIndex < 0)
                {
                    itemIndex = ~itemIndex;
                    listToInsertInto.Insert(itemIndex, item);
                }
                else
                {
                    listToInsertInto[itemIndex] = item;
                }
            }
            else
            {
                listToInsertInto.RemoveAt(itemIndex);
            }

            links = dirs.Union(files);

            WriteIndex(indexLocation, links);
        }

        private static void WriteIndex(Link indexLocation, IEnumerable<Link> links)
        {
            string filename = $"{Constants.TextFileRootDirectory}{indexLocation.Path}\\index.html";
            if (File.Exists(filename))
                File.Delete(filename);
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(fs))
            {
                foreach (var link in links)
                    writer.WriteLine($"<A HREF=\"{link.ActualFilename.Replace("\\", "/")}\">{link.DisplayedFilename}</A><BR>{link.Description}");
            }
        }
    }
}
