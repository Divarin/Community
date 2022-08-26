using miniBBS.Basic;
using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using miniBBS.Services;
using miniBBS.Services.GlobalCommands;
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
        private TextFilesSessionFlags _sessionFlags = TextFilesSessionFlags.None;

        public void Browse(BbsSession session, FilesLaunchFlags flags = FilesLaunchFlags.None)
        {
            _session = session;
            _currentLocation = _topLevel;

            var origionalShowPrompt = _session.ShowPrompt;
            var originalDnd = _session.DoNotDisturb;
            var originalLocation = _session.CurrentLocation;
            _session.CurrentLocation = Module.TextFilesBrowser;

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
                        links = ReadDirectory();

                    _session.Io.SetForeground(ConsoleColor.Gray);

                    string command;
                    if (flags.HasFlag(FilesLaunchFlags.MoveToUserHomeDirectory))
                    {
                        flags &= ~FilesLaunchFlags.MoveToUserHomeDirectory;
                        command = $"cd /CommunityUsers/{_session.User.Name}";
                    }
                    else if (flags.HasFlag(FilesLaunchFlags.ReturnToPreviousDirectory) && !string.IsNullOrWhiteSpace(_session.PreviousFilesDirectory))
                    {
                        flags &= ~FilesLaunchFlags.ReturnToPreviousDirectory;
                        command = $"cd {_session.PreviousFilesDirectory}";
                    }
                    else
                    {
                        flags &= ~FilesLaunchFlags.MoveToUserHomeDirectory;
                        flags &= ~FilesLaunchFlags.ReturnToPreviousDirectory;
                        command = Prompt(links);
                    }

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
                _session.CurrentLocation = originalLocation;
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
            var originalLocation = _session.CurrentLocation;
            _session.CurrentLocation = Module.TextFilesBrowser;

            try
            {
                _currentLocation = _topLevel;
                IList<Link> links = TopLevel.GetLinks().ToList();

                Link link = FindLink(msg, links);
                if (link != null && !link.IsDirectory)
                {
                    linkFound = true;
                    DescribeFile(link);
                    AskLaunchFile(link);
                }
                else
                    session.Io.OutputLine("Sorry I was unable to find that file.");

                return linkFound;
            }
            finally
            {
                session.DoNotDisturb = originalDnd;
                session.CurrentLocation = originalLocation;
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

            Link link = links.FirstOrDefault(l =>
                l.DisplayedFilename.Equals(parts[0], StringComparison.CurrentCultureIgnoreCase) ||
                l.ActualFilename.Replace("/index.html", "").Equals(parts[0], StringComparison.CurrentCultureIgnoreCase));
            for (int i=1; i < parts.Length && true == link?.IsDirectory; i++)
            {
                _currentLocation = link;
                links = LinkParser.GetLinksFromIndex(_session, _currentLocation);
                link = links.FirstOrDefault(l =>
                    l.DisplayedFilename.Equals(parts[i], StringComparison.CurrentCultureIgnoreCase) ||
                    l.ActualFilename.Replace("/index.html", "").Equals(parts[i], StringComparison.CurrentCultureIgnoreCase));
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
                    Message = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}"
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
                        AskLaunchFile(link);
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
                    case "cls":
                    case "clear":
                    case "c":
                        _session.Io.ClearScreen();
                        break;
                    case "grep":
                        DescriptiveDirectory(links, FilterFlags.Contents, parts.Skip(1));
                        break;
                    case "dir": 
                        DescriptiveDirectory(links, FilterFlags.Filename | FilterFlags.Description, parts.Skip(1));
                        break;
                    case "ls":
                        WideDirectory(links, FilterFlags.Filename, parts.Skip(1));
                        break;
                    case "backups":
                    case "backup":
                    case "bkups":
                    case "bkup":
                        if (_sessionFlags.HasFlag(TextFilesSessionFlags.ShowBackupFiles))
                        {
                            _sessionFlags &= ~TextFilesSessionFlags.ShowBackupFiles;
                            _session.Io.OutputLine("Hiding backup files.");
                        }
                        else
                        {
                            _sessionFlags |= TextFilesSessionFlags.ShowBackupFiles;
                            _session.Io.OutputLine("Showing backup files.");
                        }
                        result = CommandResult.ReadDirectory;
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
                    case "chl":
                    case "chanlist":
                    case "channellist":
                    case "channelist":
                        ListChannels.Execute(_session);
                        break;
                    case "link":
                        LinkFile(parts[1], links, parts.Skip(2).ToArray());
                        break;
                    case "?":
                    case "help":
                    case "man":
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
                        else
                        {
                            FileWriter.Edit(_session, _currentLocation, parts[1], links);
                            result = CommandResult.ReadDirectory;
                        }
                        break;
                    case "run":
                    case "exec":
                        if (parts.Length < 2)
                            _session.Io.OutputLine("Please supply a file name or number.");
                        else
                        {
                            var basicProgram = links.GetLink(parts[1], requireExactMatch: false);
                            if (true == basicProgram?.ActualFilename?.EndsWith(".bas", StringComparison.CurrentCultureIgnoreCase))
                                RunBasicProgram(basicProgram);
                            else
                                _session.Io.OutputLine("Invalid Basic program.");
                        }
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
                    case "rename":
                    case "ren":
                    case "rn":
                        if (parts.Length < 3)
                            _session.Io.OutputLine($"Usage: {parts[0]} (file/dir name/num) (destination)");
                        else if (_currentLocation.IsOwnedByUser(_session.User))
                        {
                            FileWriter.Rename(_session, _currentLocation, parts[1], parts[2], links);
                            result = CommandResult.ReadDirectory;
                        }
                        else
                            _session.Io.OutputLine("You may not move/rename files/directories in this directory.");
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
                    case "contrib":
                    case "editor":
                    case "uncontrib":
                    case "uneditor":
                        if (parts.Length < 3)
                            _session.Io.OutputLine($"Usage: {parts[0]} (filename/number) (username)");
                        else if (_currentLocation.IsOwnedByUser(_session.User))
                        {
                            bool add =
                                "contrib".Equals(parts[0], StringComparison.CurrentCultureIgnoreCase) ||
                                "editor".Equals(parts[0], StringComparison.CurrentCultureIgnoreCase);

                            Publisher.SetEditor(_session, _currentLocation, parts[1], parts[2], add, links);
                            result = CommandResult.ReadDirectory;
                        }
                        else
                            _session.Io.OutputLine("You may not alter contributors to files in this directory.");
                        break;
                    case "xfer":
                    case "sx":
                    case "download":
                    case "transfer":
                    case "xmodem":
                    case "send":
                        if (parts.Length >= 2)
                            SendFile(LinkExtensions.GetLink(links, parts[1]));
                        break;
                    case "rx":
                    case "upload":
                    case "receive":
                        if (parts.Length >= 2)
                            ReceiveFile(parts[1], links);
                        result = CommandResult.ReadDirectory;
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
                                AskLaunchFile(link);
                            }
                        }
                        else
                            _session.Io.OutputLine("Unrecognized command.  Use '?' or 'help' for help.");
                        break;
                }
            }

            return result;
        }

        private void AskLaunchFile(Link link)
        {
            var originalDnd = _session.DoNotDisturb;
            _session.DoNotDisturb = true;

            try
            {
                if (link.ActualFilename.EndsWith(".bas", StringComparison.CurrentCultureIgnoreCase))
                {
                    var inp = _session.Io.Ask("Run Basic Program?");
                    if (inp == 'Y')
                        RunBasicProgram(link);
                }
                else if (link.ActualFilename.EndsWith(".db", StringComparison.CurrentCultureIgnoreCase))
                {
                    var inp = _session.Io.Ask("Run SQL engine on this database?");
                    if (inp == 'Y')
                        FileWriter.Edit(_session, _currentLocation, link);
                }
                else
                {
                    var inp = _session.Io.Ask("Read file? (Y)es, (N)o, (C)ontinuous");
                    if (inp == 'Y' || inp == 'C')
                        ReadFile(link, nonstop: inp == 'C');
                }
            }
            finally
            {
                _session.DoNotDisturb = originalDnd;
            }
        }

        private void LinkFile(string filenameOrNumber, IList<Link> links, params string[] args)
        {
            string channelNameOrNumber = args.Length >= 1 ? args[0] : null;

            if (string.IsNullOrWhiteSpace(filenameOrNumber))
                return;

            Channel channel = 
                string.IsNullOrWhiteSpace(channelNameOrNumber) ?
                _session.Channel :
                GetChannel.Execute(_session, channelNameOrNumber);

            if (channel == null)
            {
                _session.Io.OutputLine($"Invalid channel name or number: {channelNameOrNumber}.");
                return;
            }
            else if (channel.Id != _session.Channel.Id && !SwitchOrMakeChannel.Execute(_session, channel.Name, allowMakeNewChannel: false))
            {
                _session.Io.OutputLine($"Unable to change to channel {channel.Name}.");
                return;
            }

            if (int.TryParse(filenameOrNumber, out int n))
            {
                if (n >= 1 && n <= links.Count)
                    LinkFile(links[n - 1], channel);
                else
                    _session.Io.OutputLine("Invalid file number");
            }
            else
            {
                var link = links.FirstOrDefault(l => l.DisplayedFilename.Equals(filenameOrNumber, StringComparison.CurrentCultureIgnoreCase));
                if (link != null)
                    LinkFile(link, channel);
                else
                    _session.Io.OutputLine("Invalid filename");
            }
        }

        private void LinkFile(Link link, Channel channel)
        {
            if (link.IsDirectory)
            {
                _session.Io.OutputLine($"{link.DisplayedFilename} is a directory, you may only link files.");
                return;
            }

            using (_session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                _session.Io.Output($"Post a link to {link.DisplayedFilename} on channel {channel.Name}?: ");
                var k = _session.Io.InputKey();
                _session.Io.OutputLine();
                if (k == 'y' || k == 'Y')
                {
                    string msg = link.DisplayedFilename.EndsWith(".bas", StringComparison.CurrentCultureIgnoreCase) ?
                        $"Basic Program Link: [{link.Parent.Path}{link.Path}].  Use '/run' to run this program. {Environment.NewLine}{link.Description}" :
                        $"TextFile Link: [{link.Parent.Path}{link.Path}].  Use '/textread' or '/tr' to read this file. {Environment.NewLine}{link.Description}";

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
                string owner = link.GetOwningUser();
                if (!string.IsNullOrWhiteSpace(owner))
                    _session.Io.OutputLine($"Owner: {owner}");
                if (true == link.Editors?.Any())
                    _session.Io.OutputLine($"Editors: {string.Join(", ", link.Editors)}");
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
            if (link.ActualFilename.FileExtension().Equals("bas", StringComparison.CurrentCultureIgnoreCase))
            {
                if (true == link.Description?.EndsWith(Constants.BasicSourceProtectedFlag))
                    body = "Source code protected from viewing.";
                else
                    body = MutantBasic.Decompress(body);
            }
            body = ReplaceLinefeedsWithEnters(body);
            var previousLocation = _session.CurrentLocation;
            _session.CurrentLocation = Module.TextFileReader;

            try
            {
                using (_session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Green))
                {
                    var flags = OutputHandlingFlag.DoNotTrimStart;

                    if (nonstop)
                        flags |= OutputHandlingFlag.Nonstop;
                    else
                        flags |= OutputHandlingFlag.PauseAtEnd;
                    _session.Io.OutputLine(body, flags);
                    _session.Io.SetForeground(ConsoleColor.Magenta);
                    _session.Io.OutputLine($"You have just read '{link.DisplayedFilename}'.");
                }
            } 
            finally
            {
                _session.CurrentLocation = previousLocation;
            }
        }

        private void SendFile(Link link)
        {
            if (link == null)
            {
                _session.Io.OutputLine("File not found");
                return;
            }

            _session.Io.OutputLine($"Sending {link.DisplayedFilename} via X-Modem protocol, begin receiving now.");

            var xfer = GlobalDependencyResolver.Get<IFileTransferProtocol>();
            var str = FileReader.LoadFileContents(_currentLocation, link);
            var data = Encoding.ASCII.GetBytes(str);
            xfer.Data = data;
            var options = FileTransferProtocolOptions.XmodemCrc;// | FileTransferProtocolOptions.Xmodem1k;
            bool sentAllData = xfer.Send(_session, options);

            _session.Io.OutputLine($"Sent {link.DisplayedFilename} {(sentAllData ? "successfully" : "unsuccessfully")}.");
        }

        private void ReceiveFile(string filename, IList<Link> links)
        {
            if (links.Any(l => l.DisplayedFilename.Equals(filename, StringComparison.CurrentCultureIgnoreCase)))
            {
                _session.Io.OutputLine("Can't overwrite existing file.");
                return;
            }

            Func<byte[]> getData = () =>
            {
                byte[] data = null;
                var xfer = GlobalDependencyResolver.Get<IFileTransferProtocol>();
                var options = FileTransferProtocolOptions.XmodemCrc;// | FileTransferProtocolOptions.Xmodem1k;
                if (xfer.Receive(_session, options))
                    data = xfer.Data;
                return data;
            };

            _session.Io.OutputLine($"Receiving {filename} via X-Modem protocol, begin sending now.");

            FileWriter.WriteUploadedData(_session, _currentLocation, filename, getData);
        }

        private void RunBasicProgram(Link link)
        {
            string body = FileReader.LoadFileContents(_currentLocation, link);
            var previousLocation = _session.CurrentLocation;
            _session.CurrentLocation = Module.BasicInterpreter;

            try
            {
                ITextEditor basic = new MutantBasic(StringExtensions.JoinPathParts(Constants.TextFileRootDirectory, link.Path) + "/", autoStart: true);
                basic.EditText(_session, new LineEditorParameters
                {
                    Filename = link.DisplayedFilename,
                    PreloadedBody = body
                });
            }
            finally
            {
                _session.CurrentLocation = previousLocation;
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
            if (string.IsNullOrWhiteSpace(dirNameOrNumber))
                return;

            if (dirNameOrNumber.Length > 1 && (dirNameOrNumber.StartsWith("/") || dirNameOrNumber.StartsWith("\\")))
            {
                ChangeDirectory("/", links);
                links = ReadDirectory();
                dirNameOrNumber = dirNameOrNumber.Substring(1);
            }

            var dirs = dirNameOrNumber.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (dirs.Length > 1)
            {
                dirNameOrNumber = dirs[0];
            }

            if (int.TryParse(dirNameOrNumber, out int n) && n >= 1 && n <= links.Count)
                ChangeDirectory(links[n - 1]);
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
                        linkNum = links.IndexOf(l => l.ActualFilename.Replace("/index.html", "").Equals(dirNameOrNumber, StringComparison.CurrentCultureIgnoreCase));
                    if (linkNum < 0)
                        linkNum = links.IndexOf(l => l.Path.Replace("/", "").Equals(dirNameOrNumber, StringComparison.CurrentCultureIgnoreCase));

                    if (linkNum < 0)
                        _session.Io.OutputLine("Invalid file/directory name.");
                    else
                        ChangeDirectory(links[linkNum]);
                }
            }

            if (dirs.Length > 1)
            {
                var next = string.Join("/", dirs.Skip(1));
                links = ReadDirectory();

                ChangeDirectory(next, links);
            }
        }

        private IList<Link> ReadDirectory()
        {
            return string.IsNullOrWhiteSpace(_currentLocation.DisplayedFilename) ?
                TopLevel.GetLinks().ToList() :
                LinkParser.GetLinksFromIndex(_session, _currentLocation, includeBackups: _sessionFlags.HasFlag(TextFilesSessionFlags.ShowBackupFiles));
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
                _session.PreviousFilesDirectory = _currentLocation.Path;
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
                _session.Io.Output($"[FILES] {Constants.InlineColorizer}{(int)ConsoleColor.Yellow}{Constants.InlineColorizer}{_currentLocation.Path}{Constants.InlineColorizer}-1{Constants.InlineColorizer}> ");
            }
        }

        private string Prompt(IList<Link> links)
        {
            ShowPrompt();

            Func<string, string> autoComplete = _l =>
            {
                var potentialFilename = _l?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                if (!string.IsNullOrWhiteSpace(potentialFilename))
                {
                    string match = links?.FirstOrDefault(l => l.DisplayedFilename.StartsWith(potentialFilename, StringComparison.CurrentCultureIgnoreCase))?.DisplayedFilename;
                    if (!string.IsNullOrWhiteSpace(match))
                    {
                        match = '\b'.Repeat(potentialFilename.Length) + match;
                        return match;
                    }
                }
                return string.Empty;
            };

            string line = _session.Io.InputLine(autoComplete, InputHandlingFlag.AutoCompleteOnTab);
            _session.Io.OutputLine();
            return line;
        }

    }
}
