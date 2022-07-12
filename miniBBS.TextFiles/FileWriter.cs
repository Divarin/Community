using miniBBS.Core;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using miniBBS.Services;
using miniBBS.TextFiles.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace miniBBS.TextFiles
{
    public static class FileWriter
    {
        public static void Edit(BbsSession session, Link currentLocation, string filenameOrNumber, IList<Link> links)
        {
            if (string.IsNullOrWhiteSpace(filenameOrNumber))
                return;

            Link file;
            bool isNewFile = false;

            if (int.TryParse(filenameOrNumber, out int n) && n >= 1 && n <= links.Count)
                file = links[n - 1];
            else
            {
                if (!ValidateFileOrDirectoryName(filenameOrNumber))
                {
                    session.Io.OutputLine("Invalid filename name");
                    return;
                }
                file = links.FirstOrDefault(l => l.DisplayedFilename.Equals(filenameOrNumber, StringComparison.CurrentCultureIgnoreCase));

                if (file == null)
                {
                    // create a new file
                    isNewFile = true;
                    file = new Link
                    {
                        ActualFilename = filenameOrNumber,
                        DisplayedFilename = filenameOrNumber,
                        Parent = currentLocation
                    };
                }
            }

            if (file == null || file.IsDirectory)
            {
                session.Io.OutputLine("Invalid filename or number.");
                return;
            }

            string body = null;
            if (!isNewFile)
                body = FileReader.LoadFileContents(currentLocation, file);

            var editor = GlobalDependencyResolver.Get<ITextEditor>();
            editor.OnSave = (UpdatedBody) =>
            {
                // save file
                SaveFile(currentLocation, file, UpdatedBody);

                session.Io.OutputLine($"Current Description: {file.Description}");
                session.Io.Output($"Enter description{(string.IsNullOrEmpty(file.Description) ? "" : " Enter=Keep Current")}: ");
                string descr = session.Io.InputLine();
                if (!string.IsNullOrWhiteSpace(descr))
                    file.Description = descr;

                UpdateIndex(currentLocation, file);
                return $"Saved as '{file.DisplayedFilename}'";
            };

            editor.EditText(session, body);
        }

        public static void Delete(BbsSession session, Link currentLocation, string filenameOrNumber, IList<Link> links, bool directory)
        {
            if (string.IsNullOrWhiteSpace(filenameOrNumber))
                return;

            Link file;
            if (int.TryParse(filenameOrNumber, out int n) && n >= 1 && n <= links.Count)
                file = links[n - 1];
            else
                file = links.FirstOrDefault(l => l.DisplayedFilename.Equals(filenameOrNumber, StringComparison.CurrentCultureIgnoreCase));

            if (file == null)
                session.Io.OutputLine("File not found.");
            else if (file.IsDirectory && !directory)
                session.Io.OutputLine("Use 'rmdir' or 'deltree' to delete directories.");
            else if (!file.IsDirectory && directory)
                session.Io.OutputLine("Use 'rm' or 'del' to delete files.");
            else
            {
                char c = session.Io.Ask($"Are you sure you want to delete '{file.DisplayedFilename}'?  This cannot be undone!");
                if (c == 'y' || c == 'Y')
                {
                    if (directory)
                        DeleteDirectory(currentLocation, file); 
                    else
                        DeleteFile(currentLocation, file);
                    session.Io.OutputLine($"{file.DisplayedFilename} deleted");
                }
            }
        }

        private static void DeleteFile(Link currentLocation, Link file)
        {
            string filename = $"{Constants.TextFileRootDirectory}{currentLocation.Path}{file.Path}";
            if (File.Exists(filename))
            {
                File.Delete(filename);
                UpdateIndex(currentLocation, file, delete: true);
            }
        }

        private static void DeleteDirectory(Link currentLocation, Link directory)
        {
            string directoryName = $"{Constants.TextFileRootDirectory}{directory.Path.Replace("index.html", "")}";
            if (Directory.Exists(directoryName))
            {
                Directory.Delete(directoryName, recursive: true);
                UpdateIndex(currentLocation, directory, delete: true);
            }
        }

        private static bool ValidateFileOrDirectoryName(string name)
        {
            if (!name.IsPrintable())
                return false;
            if (name.Length > Constants.MaxUserFilenameLength)
                return false;
            if (int.TryParse(name, out int _))
                return false;
            if (!name.All(c => char.IsLetter(c) || char.IsDigit(c) || c == '.'))
                return false;
            if (name.Count(c => c == '.') > 1)
                return false;
            if (name[0] == '.')
                return false;
            if ("index.html".Equals(name, StringComparison.CurrentCultureIgnoreCase))
                return false;
            return true;
        }

        public static void MakeDirectory(BbsSession session, Link currentLocation, string directoryName)
        {
            if (!ValidateFileOrDirectoryName(directoryName))
            {
                session.Io.OutputLine("Illegal directory name");
                return;
            }
            string dirname = $"{Constants.TextFileRootDirectory}{currentLocation.Path}{directoryName}";
            if (Directory.Exists(dirname))
            {
                session.Io.OutputLine("Directory already exists.");
                return;
            }

            if (File.Exists(dirname))
            {
                session.Io.OutputLine("Cannot create directory because a file by the same name exists in this directory.");
                return;
            }

            // get directory description
            session.Io.OutputLine("Enter directory description (blank=abort):");
            var description = session.Io.InputLine();
            if (!description.IsPrintable())
                return;

            // create the directory
            var di = new DirectoryInfo(dirname);
            di.Create();

            // create a blank index file
            string filename = dirname + "\\index.html";
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {

            }

            // add the directory and its description to the parent directory's index.html
            var newItem = new Link
            {
                DisplayedFilename = directoryName,
                ActualFilename = directoryName + "\\index.html",
                Description = description,
                Parent = currentLocation
            };

            UpdateIndex(currentLocation, newItem);
            
            session.Io.OutputLine("Directory created");
        }

        private static void SaveFile(Link currentLocation, Link file, string body)
        {
            string filename = $"{Constants.TextFileRootDirectory}{currentLocation.Path}{file.Path}";
            if (File.Exists(filename))
                File.Delete(filename);

            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.Write(body);
            }
        }

        private static void UpdateIndex(Link indexLocation, Link item, bool delete = false)
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
