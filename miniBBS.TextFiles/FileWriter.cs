﻿using miniBBS.Basic;
using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using miniBBS.Services;
using miniBBS.TextFiles.Extensions;
using miniBBS.TextFiles.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace miniBBS.TextFiles
{
    public static class FileWriter
    {
        private const string _legalFilenameChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.-_~+=()";

        /// <summary>
        /// [key = file path] value = username currently editing
        /// </summary>
        private static ConcurrentDictionary<string, string> _editLocks = new ConcurrentDictionary<string, string>();

        public static void WriteUploadedData(BbsSession session, Link currentLocation, string filename, Func<byte[]> getData)
        {
            bool isOwner = currentLocation.IsOwnedByUser(session.User);
            if (!isOwner)
            {
                session.Io.OutputLine("Access denied.");
                return;
            }

            var link = MakeNewFile(currentLocation, filename);
            var data = getData();
            if (true != data?.Any())
            {
                session.Io.OutputLine("Upload failed.");
                return;
            }

            // save file
            SaveFile(currentLocation, link, data);

            // check if stream is still in a valid state because we might call this on a connection failure
            if (true == session.Stream?.CanWrite && true == session.Stream?.CanRead)
            {
                session.Io.Output("Enter description, Enter=Keep Unpublished: ");
                string descr = session.Io.InputLine();
                if (!string.IsNullOrWhiteSpace(descr))
                {
                    link.Description = descr;
                    IndexUpdater.UpdateIndex(currentLocation, link);
                }
            }

            session.Io.OutputLine($"Uploaded '{link.DisplayedFilename}' successfully!");
        }

        public static void Edit(BbsSession session, Link currentLocation, string filenameOrNumber, IList<Link> links)
        {
            if (string.IsNullOrWhiteSpace(filenameOrNumber))
            {
                session.Io.OutputLine("Please supply a file name or number to edit.");
                return;
            }

            Link link = LinkExtensions.GetLink(links, filenameOrNumber);

            bool isNewFile = false;
            bool isOwner = currentLocation.IsOwnedByUser(session.User);
            if (link == null && isOwner)
            {
                // create a new file
                isNewFile = true;
                link = MakeNewFile(currentLocation, filenameOrNumber);
            }

            Edit(session, currentLocation, link, isNewFile);
        }

        public static void Edit(BbsSession session, Link currentLocation, Link link, bool isNewFile = false)
        {
            var originalLocation = session.CurrentLocation;
            session.CurrentLocation = Module.TextFileEditor;

            try
            {
                bool isOwner = currentLocation.IsOwnedByUser(session.User);
                if (link == null || link.IsDirectory)
                {
                    session.Io.OutputLine("Invalid filename or number.");
                    return;
                }

                bool isEditor = link.IsEditor(session.User);

                if (!isOwner && !isEditor)
                {
                    session.Io.OutputLine("Access denied.");
                    return;
                }

                string body = null;
                ITextEditor editor = GetEditor(link);

                if (!isNewFile && !(editor is ISqlUi))
                    body = FileReader.LoadFileContents(currentLocation, link);

                editor.OnSave = (UpdatedBody) =>
                {
                    // save file
                    SaveFile(currentLocation, link, UpdatedBody);

                    // check if stream is still in a valid state because we might call this on a connection failure
                    if (true == session.Stream?.CanWrite && true == session.Stream?.CanRead)
                        {
                            session.Io.OutputLine($"Current Description: {link.Description}");
                            session.Io.Output($"Enter description{(string.IsNullOrEmpty(link.Description) ? "" : " Enter=Keep Current")}: ");
                            string descr = session.Io.InputLine();
                            if (!string.IsNullOrWhiteSpace(descr))
                            {
                                link.Description = descr;
                                IndexUpdater.UpdateIndex(currentLocation, link);
                            }
                        }

                    return $"Saved as '{link.DisplayedFilename}'";
                };

                string lockKey = $"{currentLocation.Path}{link.Path}";
                if (_editLocks.ContainsKey(lockKey))
                {
                    session.Io.OutputLine($"Sorry this file is currently being edited by {_editLocks[lockKey]}.");
                    return;
                }

                try
                {
                    _editLocks.TryAdd(lockKey, session.User.Name);
                    if (editor is ISqlUi)
                        (editor as ISqlUi).Execute(session, StringExtensions.JoinPathParts(Constants.TextFileRootDirectory, currentLocation.Path, link.Path));
                    else
                        editor.EditText(session, new LineEditorParameters
                        {
                            Filename = link.DisplayedFilename,
                            PreloadedBody = body
                        });
                }
                finally
                {
                    _editLocks.TryRemove(lockKey, out string _);
                }
            }
            finally
            {
                session.CurrentLocation = originalLocation;
            }
        }

        private static ITextEditor GetEditor(Link file)
        {
            ITextEditor result;

            // GlobalDependencyResolver.Get<ISqlUi>().Execute(_session, StringExtensions.JoinPathParts(Constants.TextFileRootDirectory, _currentLocation.Path, parts[1]));
            switch (file.ActualFilename.FileExtension().ToLower())
            {
                case "bas":
                    result = new MutantBasic(StringExtensions.JoinPathParts(Constants.TextFileRootDirectory, file.Path) + "/", autoStart: false);
                    break;
                case "db":
                    result = GlobalDependencyResolver.Get<ISqlUi>() as ITextEditor;
                    break;
                default:
                    result = GlobalDependencyResolver.Get<ITextEditor>();
                    break;
            }

            return result;
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

        public static void Rename(BbsSession session, Link currentLocation, string linkNameOrNum, string destName, IList<Link> links)
        {
            if (string.IsNullOrWhiteSpace(linkNameOrNum))
                return;

            if (!ValidateFileOrDirectoryName(destName))
            {
                session.Io.OutputLine($"Invalid destination name '{destName}'.");
                return;
            }

            Link link;
            if (int.TryParse(linkNameOrNum, out int n) && n >= 1 && n <= links.Count)
                link = links[n - 1];
            else
                link = links.FirstOrDefault(l => l.DisplayedFilename.Equals(linkNameOrNum, StringComparison.CurrentCultureIgnoreCase));

            if (link == null)
                session.Io.OutputLine("File or Directory not found.");
            else
            {
                string src = link.IsDirectory ?
                    $"{Constants.TextFileRootDirectory}{link.Path}" :
                    $"{Constants.TextFileRootDirectory}{currentLocation.Path}{link.Path}";

                string dst = $"{Constants.TextFileRootDirectory}{currentLocation.Path}{destName}";

                if (File.Exists(dst) || Directory.Exists(dst))
                    session.Io.OutputLine($"A file or directory by the name of {destName} already exists.");
                else if (link.IsDirectory)
                {
                    Directory.Move(src, dst);
                    session.Io.OutputLine($"Renamed directory '{link.DisplayedFilename}' to '{destName}'.");
                }
                else
                {
                    File.Move(src, dst);
                    session.Io.OutputLine($"Renamed file '{link.DisplayedFilename}' to '{destName}'.");
                }
                IndexUpdater.UpdateIndex(currentLocation, link, delete: true);
                if (link.IsDirectory)
                    link.ActualFilename = destName + "/index.html";
                else
                    link.ActualFilename = destName;
                link.DisplayedFilename = destName;
                IndexUpdater.UpdateIndex(currentLocation, link, delete: false);

            }
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

            IndexUpdater.UpdateIndex(currentLocation, newItem);
            
            session.Io.OutputLine("Directory created");
        }

        private static void DeleteFile(Link currentLocation, Link file)
        {
            string filename = $"{Constants.TextFileRootDirectory}{currentLocation.Path}{file.Path}";
            if (File.Exists(filename))
            {
                File.Delete(filename);
                IndexUpdater.UpdateIndex(currentLocation, file, delete: true);
            }
        }

        private static void DeleteDirectory(Link currentLocation, Link directory)
        {
            string directoryName = $"{Constants.TextFileRootDirectory}{directory.Path.Replace("index.html", "")}";
            if (Directory.Exists(directoryName))
            {
                Directory.Delete(directoryName, recursive: true);
                IndexUpdater.UpdateIndex(currentLocation, directory, delete: true);
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
            if (!name.All(c => _legalFilenameChars.Contains(c)))
                return false;
            if (name.Count(c => c == '.') > 1)
                return false;
            if (name[0] == '.')
                return false;
            if ("index.html".Equals(name, StringComparison.CurrentCultureIgnoreCase))
                return false;
            return true;
        }

        /// <summary>
        /// Write text data (also makes backups)
        /// </summary>
        private static void SaveFile(Link currentLocation, Link file, string body)
        {
            string filename = $"{Constants.TextFileRootDirectory}{currentLocation.Path}{file.Path}";
            if (File.Exists(filename))
            {
                try
                {
                    File.Move(filename, GetBackupFilename(filename));
                }
                catch
                {
                    File.Delete(filename);
                }
            }

            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.Write(body);
            }
        }

        /// <summary>
        /// write binary data (no backups made)
        /// </summary>
        private static void SaveFile(Link currentLocation, Link file, byte[] data)
        {
            string filename = $"{Constants.TextFileRootDirectory}{currentLocation.Path}{file.Path}";
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))            
            {
                foreach (byte b in data)
                    fs.WriteByte(b);
            }
        }

        private static string GetBackupFilename(string filename)
        {
            int i = 1;
            while (File.Exists($"{filename}.bkup{i}") && i < Constants.MaxFileBackups)
                i++;
            if (i > Constants.MaxFileBackups || (i == Constants.MaxFileBackups && File.Exists($"{filename}.bkup{i}")))
            {
                for (int f=1; f < i; f++)
                {
                    File.Delete($"{filename}.bkup{f}");
                    File.Move($"{filename}.bkup{f + 1}", $"{filename}.bkup{f}");
                }
            }
            
            return $"{filename}.bkup{i}";
        }

        private static Link MakeNewFile(Link currentLocation, string filename)
        {
            return new Link
            {
                ActualFilename = filename,
                DisplayedFilename = filename,
                Parent = currentLocation
            };
        }
    }
}
