using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using miniBBS.Services;
using miniBBS.TextFiles.Enums;
using miniBBS.TextFiles.Extensions;
using miniBBS.TextFiles.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.TextFiles
{
    public class TextFilesBrowser : ITextFilesBrowser
    {
        private BbsSession _session;
        private Link _currentLocation;
        private readonly Link _topLevel = new Link
        {
            ActualFilename = "/",
            Description = "Root directory",
            DisplayedFilename = string.Empty
        };

        public Action<string> OnChat { get; set; }

        public void Browse(BbsSession session)
        {
            _session = session;
            _currentLocation = _topLevel;

            var origionalShowPrompt = _session.ShowPrompt;
            var originalDnd = _session.DoNotDisturb;

            try
            {
                // replace the prompt so that after ping/pongs and other notifications 
                // we'll see the textfiles browser's prompt instead of community's
                _session.ShowPrompt = () =>
                {
                    if (_session.PingType == PingPongType.Full)
                        ShowPrompt();
                };

                if (!_session.DoNotDisturb)
                {
                    _session.DoNotDisturb = true;
                    _session.Io.OutputLine("Do not disturb mode is now ON.  Type 'dnd' to toggle this.");
                }

                CommandResult cmd = CommandResult.ReadDirectory;
                IList<Link> links = null;

                while (cmd != CommandResult.ExitSystem)
                {
                    if (cmd == CommandResult.ReadDirectory)
                    {
                        links = string.IsNullOrWhiteSpace(_currentLocation.DisplayedFilename) ?
                            TopLevel.GetLinks().ToList() :
                            LinkParser.GetLinksFromIndex(session, _currentLocation);
                    }

                    _session.Io.SetForeground(ConsoleColor.Gray);

                    string command = Prompt();
                    cmd = ProcessCommand(command, links);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    HandleException(ex);
                }
                catch
                {
                    throw;
                }
            }
            finally
            {
                _session.ShowPrompt = origionalShowPrompt;
                _session.DoNotDisturb = originalDnd;
            }
        }

        /// <summary>
        /// Returns true if a link was found in the <paramref name="msg"/>
        /// </summary>
        public bool ReadLink(BbsSession session, string msg)
        {
            bool linkFound = false;
            _session = session;
            var originalDnd = session.DoNotDisturb;

            try
            {
                _currentLocation = _topLevel;
                IList<Link> links = TopLevel.GetLinks().ToList();

                Link link = FindLink(msg, links);
                if (link != null && !link.IsDirectory)
                {
                    linkFound = true;
                    DescribeFile(link);
                    var inp = _session.Io.Ask("Read file? (Y)es, (N)o, (C)ontinuous");
                    if (inp == 'Y' || inp == 'C')
                        ReadFile(link, nonstop: inp == 'C');
                }
                else
                    session.Io.OutputLine("Sorry I was unable to find that file.");

                return linkFound;
            }
            finally
            {
                session.DoNotDisturb = originalDnd;
            }
        }

        private Link FindLink(string msg, IList<Link> links)
        {
            if (string.IsNullOrWhiteSpace(msg))
                return null;
            int pos = msg.IndexOf("[");
            if (pos < 0)
                return null;
            pos++;
            int end = msg.IndexOf("]", pos);
            if (end <= pos)
                return null;
            int len = end - pos;
            string path = msg.Substring(pos, len);
            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            Link link = links.FirstOrDefault(l => l.DisplayedFilename.Equals(parts[0], StringComparison.CurrentCultureIgnoreCase));
            for (int i=1; i < parts.Length && true == link?.IsDirectory; i++)
            {
                _currentLocation = link;
                links = LinkParser.GetLinksFromIndex(_session, _currentLocation);
                link = links.FirstOrDefault(l => l.DisplayedFilename.Equals(parts[i], StringComparison.CurrentCultureIgnoreCase));
            }

            return link;
        }

        private void HandleException(Exception ex)
        {
            ex = ex.InnermostException();
            _session.Io.OutputLine($"Something went wrong: {ex.Message}");
            _session.Io.OutputLine("Attempting to notify sysop...");

            int toId = GlobalDependencyResolver.GetRepository<Core.Models.Data.User>()
                .Get(u => u.Name, Constants.SysopName)
                .First()
                .Id;

            GlobalDependencyResolver.GetRepository<Core.Models.Data.Mail>()
                .Insert(new Core.Models.Data.Mail
                {
                    ToUserId = toId,
                    FromUserId = _session.User.Id,
                    SentUtc = DateTime.UtcNow,
                    Subject = "Exception during Textfiles browse",
                    Message = ex.Message
                });

        }

        private CommandResult ProcessCommand(string command, IList<Link> links)
        {
            var result = CommandResult.None;

            if (string.IsNullOrWhiteSpace(command))
            {
                _session.Io.OutputLine("Type '?' or 'help' for help.  Type 'Q', 'QUIT', or 'EXIT' to leave the text files browser.");
                return result;
            }

            if (int.TryParse(command, out int n))
            {
                if (n >= 1 && n <= links.Count)
                {
                    var link = links[n - 1];
                    if (link.IsDirectory)
                    {
                        DescribeFile(link);
                        if (_session.Io.Ask("Change to directory?") == 'Y')
                        {
                            ChangeDirectory(link);
                            result = CommandResult.ReadDirectory;
                        }
                    }
                    else
                    {
                        DescribeFile(link);
                        var inp = _session.Io.Ask("Read file? (Y)es, (N)o, (C)ontinuous");
                        if (inp == 'Y' || inp == 'C')
                            ReadFile(link, nonstop: inp=='C');
                    }
                }
                else
                    _session.Io.OutputLine("Invalid file/directory number.");
            }
            else
            {
                var parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                switch (parts[0].ToLower())
                {
                    case "grep":
                        DescriptiveDirectory(links, FilterFlags.Contents, parts.Skip(1));
                        break;
                    case "dir": 
                        DescriptiveDirectory(links, FilterFlags.Filename | FilterFlags.Description, parts.Skip(1));
                        break;
                    case "ls":
                        WideDirectory(links, FilterFlags.Filename, parts.Skip(1));
                        break;
                    case "chdir":
                    case "cd":
                        // change directory using chdir or cd
                        if (parts.Length >= 2)
                        {
                            ChangeDirectory(parts[1], links);
                            result = CommandResult.ReadDirectory;
                        }
                        break;
                    case "cd..":
                        ChangeDirectory(_currentLocation.Parent, goingUp: true);
                        result = CommandResult.ReadDirectory;
                        break;
                    case "cd/":
                    case "cd\\":
                        ChangeDirectory(_topLevel, goingUp: true);
                        result = CommandResult.ReadDirectory;
                        break;
                    case "/o":
                    case "q":
                    case "/q":
                    case "quit":
                    case "exit":
                        result = CommandResult.ExitSystem;
                        break;
                    case "desc":
                    case "d":
                    case "describe":
                        if (parts.Length >= 2)
                            DescribeFile(parts[1], links);
                        break;
                    case "read":
                    case "cat":
                    case "more":
                    case "less":
                    case "type":
                        if (parts.Length >= 2)
                            ReadFile(parts[1], links, nonstop: false);
                        break;
                    case "nonstop":
                    case "ns":
                    case "continuous":
                        if (parts.Length >= 2)
                            ReadFile (parts[1], links, nonstop: true);
                        break;
                    case "link":
                        if (parts.Length >= 2)
                            LinkFile(parts[1], links);
                        break;
                    case "?":
                    case "help":
                        Help.Show(_session, parts.Length >= 2 ? parts[1] : null);
                        break;
                    case "chat":
                        OnChat?.Invoke(string.Join(" ", parts.Skip(1)));
                        break;
                    case "dnd":
                        _session.DoNotDisturb = !_session.DoNotDisturb;
                        _session.Io.OutputLine($"Do not disturb mode is : {(_session.DoNotDisturb ? "On" : "Off")}");
                        break;
                    case "edit":
                    case "nano":
                        if (parts.Length < 2)
                            _session.Io.OutputLine("Please supply a file name or number.");
                        else if (_currentLocation.IsOwnedByUser(_session.User))
                        {
                            FileWriter.Edit(_session, _currentLocation, parts[1], links);
                            result = CommandResult.ReadDirectory;
                        }
                        else
                            _session.Io.OutputLine("You may not create or edit files in this directory.");
                        break;
                    case "mkdir":
                    case "md":
                        if (parts.Length < 2)
                            _session.Io.OutputLine("Please supply a directory name.");
                        else if (_currentLocation.IsOwnedByUser(_session.User) ||
                            (_session.User.Access.HasFlag(AccessFlag.Administrator) && _currentLocation.IsUserGeneratedContent()))
                        {
                            FileWriter.MakeDirectory(_session, _currentLocation, parts[1]);
                            result = CommandResult.ReadDirectory;
                        }
                        else
                            _session.Io.OutputLine("Unable to create directory.");
                        break;
                    case "del":
                    case "rd":
                    case "rm":
                        if (parts.Length < 2)
                            _session.Io.OutputLine("Please supply a file name or number.");
                        else if (_currentLocation.IsOwnedByUser(_session.User))
                        {
                            FileWriter.Delete(_session, _currentLocation, parts[1], links, directory: false);
                            result = CommandResult.ReadDirectory;
                        }
                        else
                            _session.Io.OutputLine("You may not delete files in this directory.");
                        break;
                    case "rmdir":
                    case "deltree":
                        if (parts.Length < 2)
                            _session.Io.OutputLine("Please supply a directory name or number.");
                        else if (_currentLocation.IsOwnedByUser(_session.User))
                        {
                            FileWriter.Delete(_session, _currentLocation, parts[1], links, directory: true);
                            result = CommandResult.ReadDirectory;
                        }
                        else
                            _session.Io.OutputLine("You may not delete directories in this directory.");
                        break;
                    case "publish":
                    case "pub":
                        if (parts.Length < 2)
                            _session.Io.OutputLine("Please supply a directory name or number.");
                        else if (_currentLocation.IsOwnedByUser(_session.User))
                        {
                            Publisher.Publish(_session, _currentLocation, parts[1], links);
                            result = CommandResult.ReadDirectory;
                        }
                        else
                            _session.Io.OutputLine("You may not publish files in this directory.");
                        break;
                    case "unpublish":
                    case "unpub":
                        if (parts.Length < 2)
                            _session.Io.OutputLine("Please supply a directory name or number.");
                        else if (_currentLocation.IsOwnedByUser(_session.User))
                        {
                            Publisher.Unpublish(_session, _currentLocation, parts[1], links);
                            result = CommandResult.ReadDirectory;
                        }
                        else
                            _session.Io.OutputLine("You may not publish files in this directory.");
                        break;
                        break;
                    default:
                        var link = links.FirstOrDefault(x => x.DisplayedFilename.Equals(parts[0], StringComparison.CurrentCultureIgnoreCase));
                        if (link != null)
                        {
                            if (link.IsDirectory)
                            {
                                ChangeDirectory(link);
                                result = CommandResult.ReadDirectory;
                            }
                            else
                            {
                                DescribeFile(link);
                                var inp = _session.Io.Ask("Read file? (Y)es, (N)o, (C)ontinuous");
                                if (inp == 'Y' || inp == 'C')
                                    ReadFile(link, nonstop: inp == 'C');
                            }
                        }
                        else
                            _session.Io.OutputLine("Unrecognized command.  Use '?' or 'help' for help.");
                        break;
                }
            }

            return result;
        }

        private void LinkFile(string filenameOrNumber, IList<Link> links)
        {
            if (string.IsNullOrWhiteSpace(filenameOrNumber))
                return;
            else if (int.TryParse(filenameOrNumber, out int n))
            {
                if (n >= 1 && n <= links.Count)
                    LinkFile(links[n - 1]);
                else
                    _session.Io.OutputLine("Invalid file number");
            }
            else
            {
                var link = links.FirstOrDefault(l => l.DisplayedFilename.Equals(filenameOrNumber, StringComparison.CurrentCultureIgnoreCase));
                if (link != null)
                    LinkFile(link);
                else
                    _session.Io.OutputLine("Invalid filename");
            }
        }

        private void LinkFile(Link link)
        {
            if (link.IsDirectory)
            {
                _session.Io.OutputLine($"{link.DisplayedFilename} is a directory, you may only link files.");
                return;
            }

            using (_session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                _session.Io.Output($"Post a link to {link.DisplayedFilename} on channel {_session.Channel.Name}?: ");
                var k = _session.Io.InputKey();
                _session.Io.OutputLine();
                if (k == 'y' || k == 'Y')
                {
                    string msg = $"TextFile Link: [{link.Parent.Path}{link.Path}].  Use '/textread' or '/tr' to read this file.";
                    _session.Io.SetForeground(ConsoleColor.Yellow);
                    _session.Io.OutputLine("Link posted.");
                    OnChat?.Invoke(msg);
                }
            }
        }

        private void DescribeFile(string filenameOrNumber, IList<Link> links)
        {
            if (string.IsNullOrWhiteSpace(filenameOrNumber))
                return;
            else if (int.TryParse(filenameOrNumber, out int n))
            {
                if (n >= 1 && n <= links.Count)
                    DescribeFile(links[n - 1]);
                else
                    _session.Io.OutputLine("Invalid file number");
            }
            else
            {
                var link = links.FirstOrDefault(l => l.DisplayedFilename.Equals(filenameOrNumber, StringComparison.CurrentCultureIgnoreCase));
                if (link != null)
                    DescribeFile(link);
                else
                    _session.Io.OutputLine("Invalid filename");
            }
        }

        private void DescribeFile(Link link)
        {
            using (_session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
            {
                _session.Io.OutputLine(link.Path);
                _session.Io.SetForeground(ConsoleColor.Cyan);
                _session.Io.OutputLine(link.Description);
            }
        }

        private void ReadFile(string filenameOrNumber, IList<Link> links, bool nonstop = false)
        {
            if (string.IsNullOrWhiteSpace(filenameOrNumber))
                return;
            else if (int.TryParse(filenameOrNumber, out int n))
            {
                if (n >= 1 && n <= links.Count)
                    ReadFile(links[n - 1], nonstop);
                else
                    _session.Io.OutputLine("Invalid file number");
            }
            else
            {
                var link = links.FirstOrDefault(l => l.DisplayedFilename.Equals(filenameOrNumber, StringComparison.CurrentCultureIgnoreCase));
                if (link != null)
                    ReadFile(link, nonstop);
                else
                    _session.Io.OutputLine("Invalid filename");
            }
        }

        private void ReadFile(Link link, bool nonstop = false)
        {
            if (link.IsDirectory || link == _topLevel)
            {
                _session.Io.OutputLine($"{link.DisplayedFilename} is a directory.");
                return;
            }

            string body = FileReader.LoadFileContents(_currentLocation, link);
            body = ReplaceLinefeedsWithEnters(body);

            try
            {
                _session.NoPingPong = true;
                using (_session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Green))
                {
                    var flags = OutputHandlingFlag.None;
                    if (!link.IsUserGeneratedContent()) flags |= OutputHandlingFlag.DoNotTrimStart;
                    if (nonstop) flags |= OutputHandlingFlag.Nonstop;
                    _session.Io.OutputLine(body, flags);
                }
            } 
            finally
            {
                _session.NoPingPong = false;
            }
        }

        /// <summary>
        /// Replaces standalone linefeeds with newlines.  This does not effect encodings where 
        /// newlines are followed by linefeeds.  This only affects encodings where linefeeds (char 10)
        /// are used without a preceeding newline (13)
        /// </summary>
        private string ReplaceLinefeedsWithEnters(string body)
        {
            char[] chrs = new char[body.Length];

            for (int i=0; i < body.Length; i++)
            {
                char c = body[i];
                // is this character a linefeed?
                if (c == 10)
                {
                    // yes, was the previous character *not* a newline?
                    if (i > 0 && body[i - 1] != 13)
                        c = (char)13; // then make this a newline
                }
                chrs[i] = c;
            }

            string result = new string(chrs);
            return result;
        }

        private void ChangeDirectory(string dirNameOrNumber, IList<Link> links)
        {
            if (int.TryParse(dirNameOrNumber, out int n))
            {
                if (n >= 1 && n <= links.Count)
                    ChangeDirectory(links[n - 1]);
                else
                    _session.Io.OutputLine("Invalid file/directory number.");
            }
            else
            {
                if (dirNameOrNumber == "..")
                    ChangeDirectory(_currentLocation.Parent, goingUp: true);
                else if (dirNameOrNumber == "/" || dirNameOrNumber == "\\")
                    ChangeDirectory(_topLevel, goingUp: true);
                else
                {
                    var linkNum = links.IndexOf(l => l.DisplayedFilename.Equals(dirNameOrNumber, StringComparison.CurrentCultureIgnoreCase));
                    if (linkNum < 0)
                        _session.Io.OutputLine("Invalid file/directory name.");
                    else
                        ChangeDirectory(links[linkNum]);
                }
            }
        }

        private void ChangeDirectory(Link link, bool goingUp = false)
        {
            if (link == null)
                _session.Io.OutputLine("Unable to change directory");
            else if (!link.IsDirectory && link != _topLevel)
                _session.Io.OutputLine($"'{link.ActualFilename}' is not a directory.");
            else
            {
                if (!goingUp)
                    link.Parent = _currentLocation;
                _currentLocation = link;
            }
        }

        private void DescriptiveDirectory(IList<Link> links, FilterFlags filterFlags, IEnumerable<string> filters = null)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"{Constants.InlineColorizer}{(int)ConsoleColor.Yellow}{Constants.InlineColorizer}{links.Count} total entries in {_currentLocation.ActualFilename}{Constants.InlineColorizer}-1{Constants.InlineColorizer}");

            for (int i = 0; i < links.Count; i++)
            {
                var link = links[i];
                if (true == filters?.Any() && !LinkMatchesFilters(link, filters, filterFlags))
                    continue;
                var filename = link.IsDirectory ? $"[{Constants.InlineColorizer}{(int)ConsoleColor.Blue}{Constants.InlineColorizer}{link.DisplayedFilename}{Constants.InlineColorizer}-1{Constants.InlineColorizer}]" : $"{Constants.InlineColorizer}{(int)ConsoleColor.Cyan}{Constants.InlineColorizer}{link.DisplayedFilename}{Constants.InlineColorizer}-1{Constants.InlineColorizer}";
                string line = $"{(i + 1).ToString().PadLeft(3, Constants.Spaceholder)} : {filename}, ";

                line += links[i].Description
                    .Replace(Environment.NewLine, " ");
                
                line = line.MaxLength(_session.Cols);
                builder.AppendLine(line);
            }

            _session.Io.OutputLine(builder.ToString());
        }

        private void WideDirectory(IList<Link> links, FilterFlags filterFlags, IEnumerable<string> filters = null)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"{Constants.InlineColorizer}{(int)ConsoleColor.Yellow}{Constants.InlineColorizer}{links.Count} total entries in {_currentLocation.ActualFilename}{Constants.InlineColorizer}-1{Constants.InlineColorizer}");

            for (int i = 0; i < links.Count; i++)
            {
                var link = links[i];
                if (true == filters?.Any() && !LinkMatchesFilters(link, filters, filterFlags))
                    continue;

                if (i > 0)
                    builder.Append("  ");

                var filename = link.IsDirectory ? $"[{Constants.InlineColorizer}{(int)ConsoleColor.Blue}{Constants.InlineColorizer}{link.DisplayedFilename}{Constants.InlineColorizer}-1{Constants.InlineColorizer}]" : $"{Constants.InlineColorizer}{(int)ConsoleColor.Cyan}{Constants.InlineColorizer}{link.DisplayedFilename}{Constants.InlineColorizer}-1{Constants.InlineColorizer}";
                string entry = $"{i + 1}:{filename}";

                builder.Append(entry);
            }

            _session.Io.OutputLine(builder.ToString());
        }

        private bool LinkMatchesFilters(Link link, IEnumerable<string> filters, FilterFlags flags)
        {
            // check filename and description first
            var orFilters = filters
                .Where(f => !f.Contains("&"))
                .Select(f => f.ToUpper())
                .ToArray();

            if (flags.HasFlag(FilterFlags.Filename) && orFilters.Any(f => link.DisplayedFilename.ToUpper().Contains(f)))
                return true;

            if (flags.HasFlag(FilterFlags.Description) && orFilters.Any(f => link.Description.ToUpper().Contains(f)))
                return true;

            var andFilters = filters
                .Where(f => f.Contains("&"))
                .Select(f => f.Split('&').Select(x => x.ToUpper()))
                .ToArray();

            var fileContents = new Lazy<string>(() => FileReader.LoadFileContents(_currentLocation, link).ToUpper());
            
            if (!link.IsDirectory && flags.HasFlag(FilterFlags.Contents))
            {
                if (orFilters.Any(f => fileContents.Value.Contains(f)))
                    return true;
            }

            if (andFilters.Any(f => f.All(x =>
                ( flags.HasFlag(FilterFlags.Filename) && link.DisplayedFilename.ToUpper().Contains(x) ) ||
                ( flags.HasFlag(FilterFlags.Description) && link.Description.ToUpper().Contains(x) ) ||
                (!link.IsDirectory && flags.HasFlag(FilterFlags.Contents) && fileContents.Value.Contains(x))
                )))
            {
                return true;
            }

            return false;
        }

        private void ShowPrompt()
        {
            using (_session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
            {
                _session.Io.OutputLine();
                _session.Io.Output($"[TEXTS] {Constants.InlineColorizer}{(int)ConsoleColor.Yellow}{Constants.InlineColorizer}{_currentLocation.Path}{Constants.InlineColorizer}-1{Constants.InlineColorizer}> ");
            }
        }

        private string Prompt()
        {
            ShowPrompt();
            string line = _session.Io.InputLine();
            _session.Io.OutputLine();
            return line;
        }

        //private IList<Link> GetLinksFromIndex(Link parentDirectory)
        //{
        //    string dir = Constants.TextFileRootDirectory;
        //    if (_currentLocation.Parent != null)
        //        dir += _currentLocation.Parent.Path;
        //    var txt = FileReader.ReadFile(new FileInfo(JoinPathParts(dir, _currentLocation.ActualFilename)));
        //    var links = LinkParser.GetLinks(txt).ToList();
        //    foreach (var link in links)
        //        link.Parent = parentDirectory;
        //    return links;
        //}


    }
}
