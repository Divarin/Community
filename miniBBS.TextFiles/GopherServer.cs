using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Services;
using miniBBS.Services.GlobalCommands;
using miniBBS.TextFiles.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace miniBBS.TextFiles
{
    public class GopherServer : IGopherServer
    {
        const int _requestWaitSec = 30;
        private GopherServerOptions _options;
        private ILogger _logger;
        private ConditionalWeakTable<NetworkStream, GopherServerSessionFlag> _sessionFlags =
            new ConditionalWeakTable<NetworkStream, GopherServerSessionFlag>();
        
        private static readonly Dictionary<string, char> _gopherEntryTypeDict = new Dictionary<string, char>()
        {
            {"txt", (char) GopherEntryType.TextFile},
            {"zip", (char) GopherEntryType.DosBinaryArchive},
            {"exe", (char) GopherEntryType.BinaryFile},
            {"gif", (char) GopherEntryType.GifImage},
            {"jpg", (char) GopherEntryType.Image},
            {"png", (char) GopherEntryType.PngImage},
            {"bmp", (char) GopherEntryType.PlusBitmapImage},
            {"mov", (char) GopherEntryType.PlusMovieFile},
            {"m4v", (char) GopherEntryType.PlusMovieFile},
            {"mkv", (char) GopherEntryType.PlusMovieFile},
            {"avi", (char) GopherEntryType.PlusMovieFile},
            {"mpg", (char) GopherEntryType.PlusMovieFile},
            {"mpeg", (char) GopherEntryType.PlusMovieFile},
            {"mp4", (char) GopherEntryType.PlusMovieFile},
            {"pdf", (char) GopherEntryType.PDF},
            {"xml", (char) GopherEntryType.XML},
            {"htm", (char) GopherEntryType.Html},
            {"htl", (char) GopherEntryType.Html},
            {"mid", (char) GopherEntryType.SoundFile},
            {"wav", (char) GopherEntryType.SoundFile},
            {"mp3", (char) GopherEntryType.SoundFile},
            {"doc", (char) GopherEntryType.Doc},
            {"rtf", (char) GopherEntryType.RichText},
            {"map", (char) GopherEntryType.Directory},
        };

        public void StartServer(GopherServerOptions options)
        {
            _options = options;
            _logger = GlobalDependencyResolver.Default.Get<ILogger>();

            TcpListener listener = new TcpListener(IPAddress.Any, _options.GopherServerPort);
            listener.Start();

            while (!_options.SystemControl.HasFlag(SystemControlFlag.Shutdown))
            {
                try
                {
                    var clientFactory = new TcpClientFactory(listener);
                    clientFactory.AwaitConnection();
                    while (clientFactory.Client == null)
                    {
                        Thread.Sleep(25);
                    }
                    var client = clientFactory.Client;
                    var threadStart = new ParameterizedThreadStart(BeginConnection);
                    var thread = new Thread(threadStart);
                    thread.Start(client);
                    var sw = new Stopwatch();
                    sw.Start();
                    
                    var stream = client != null && client.Connected ? client.GetStream() : null;
                    var keepWaiting = stream != null;

                    while (keepWaiting)
                    {
                        keepWaiting = client.Connected;
                        keepWaiting &= thread.ThreadState == System.Threading.ThreadState.Running;
                        if (stream == null || !stream.CanWrite)
                        {
                            keepWaiting = false;
                        }
                        else if (!this._sessionFlags.TryGetValue(stream, out var flag) ||
                            !flag.IsDownloadingBinaryContent)
                        {
                            keepWaiting &= sw.Elapsed.TotalSeconds < _requestWaitSec;
                        }
                        Thread.Sleep(25);
                    };

                    if (client.Connected && thread.ThreadState == System.Threading.ThreadState.Running)
                    {
                        client.Close();
                        thread.Abort();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(null, $"{DateTime.Now} - {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                }
            }
            listener?.Stop();
        }

        private void BeginConnection(object obj)
        {
            var client = obj as TcpClient;

            try
            {
                using (var stream = client.GetStream())
                {
                    var buffer = new byte[2048];
                    var builder = new StringBuilder();
                    while (stream.CanRead)
                    {
                        var bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                            builder.Append(new string(buffer.Select(b => (char)b).ToArray(), 0, bytesRead));
                        if (buffer.Any(x => x == 13))
                            break;
                    }
                    var request = builder.ToString();

                    var ip = (client.Client.RemoteEndPoint as IPEndPoint)?.Address?.ToString();
                    SysopScreen.RegisterGopherServerRequest(ip, request.Replace("\r", "").Replace("\n", ""));

                    var requestWithoutNewline = request.Replace("\r", "").Replace("\n", "");

                    if (requestWithoutNewline.StartsWith("/Radio", StringComparison.CurrentCultureIgnoreCase) &&
                        requestWithoutNewline.EndsWith(".mp3", StringComparison.CurrentCultureIgnoreCase))
                    {
                        WriteFileContents($"z:{requestWithoutNewline}", stream);
                    }
                    else
                    {
                        var response = $"{GetResponse(request)}{Environment.NewLine}.{Environment.NewLine}";
                        var responseBytes = Encoding.ASCII.GetBytes(response);
                        stream.Write(responseBytes, 0, responseBytes.Length);
                    }

                    Thread.Sleep(250);
                    stream.Close();
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.Log(null, ex.Message, LoggingOptions.ToDatabase | LoggingOptions.WriteImmedately);
            }
            finally
            {
                client?.Close();
            }
        }

        private string GetResponse(string selector)
        {
            if (string.IsNullOrWhiteSpace(selector) || selector.Trim() == "/")
                return GetRootResponse();

            string input = null;
            selector = selector.Replace("\r", "").Replace("\n", "");
            if (selector.Contains('\t'))
            {
                var p = selector.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length == 2)
                {
                    selector = p[0];
                    input = p[1];
                }
            }
            var parts = selector.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 1 && "Radio".Equals(parts[0], StringComparison.CurrentCultureIgnoreCase))
            {
                return ReadRadioDirectory(parts.Skip(1).ToArray());
            }

            if (parts.Length == 1 && !"index.html".Equals(parts[0], StringComparison.CurrentCultureIgnoreCase))
            {
                // read file from users root directory
                var filename = $"{Constants.TextFileRootDirectory}users/{parts[0]}";
                if (!File.Exists(filename))
                    return Info("File not found");

                return ReadFile(filename);
            }

            var isDevNotes =
                parts.Length >= 3 &&
                "users".Equals(parts[0], StringComparison.CurrentCultureIgnoreCase) &&
                "Divarin".Equals(parts[1], StringComparison.CurrentCultureIgnoreCase) &&
                "DevNotes".Equals(parts[2], StringComparison.CurrentCultureIgnoreCase);

            if (!isDevNotes && (parts.Length < 3 || !"pub".Equals(parts[2], StringComparison.CurrentCultureIgnoreCase)))
            {
                // trying to access dir or file outside of a pub directory.
                return Info("File not found");
            }

            var path = string.Join("/", parts.Where(x => x != ".." && x != "\\" && !"index.html".Equals(x, StringComparison.CurrentCultureIgnoreCase)));
            var pathWithRoot = $"{Constants.TextFileRootDirectory}{path}";

            if (Directory.Exists(pathWithRoot))
            {
                // get contents of user directory
                var link = new Models.Link { ActualFilename = path + "/index.html" };
                var links = LinkParser.GetLinksFromIndex(null, link);
                var response = string.Join(Environment.NewLine, links.Select(l => BuildLine(l)));
                var topLevelLine = $"{(char)GopherEntryType.Directory}{Constants.BbsName} Gopher Root\t/\t{Constants.Hostname}\t{_options.GopherServerPort}{Environment.NewLine}";
                response = $"{topLevelLine}{response}";
                return response;
            }
            else if (File.Exists(pathWithRoot) && IsPublishedFile(parts))
            {
                // read file from a user subdirectory
                var response = ReadFile(pathWithRoot);
                return response;
            }

            return Info("File not found");
        }

        private string ReadRadioDirectory(string[] pathParts)
        {
            var root = string.Join("\\", pathParts
                .Where(x => x.All(c => c == ' ' || char.IsLetterOrDigit(c))));
            string path = $"z:\\{root}";
            var di = new DirectoryInfo(path);
            var dirs = di.GetDirectories()
                .Select(x => new Models.Link
                {
                    DisplayedFilename = x.Name,
                    ActualFilename = x.Name + "/index.html",
                    //Description = x.Name,
                    Parent = new Models.Link
                    {
                        ActualFilename = "Radio/index.html",
                    },
                })
                .OrderBy(x => x.DisplayedFilename)
                .ToList();

            var files = di
                .GetFiles()
                .Select(x => new Models.Link
                {
                    DisplayedFilename = x.Name,
                    ActualFilename = x.Name,
                    Description = $"{x.Length:###,###,###,###} bytes",
                    Parent = new Models.Link
                    {
                        ActualFilename = $"Radio/{root}/index.html",
                    },
                })
                .OrderByDescending(x => x.DisplayedFilename)
                .ToList();

            var dirsAndFiles = dirs.Union(files).ToList();

            var response = string.Join(Environment.NewLine, dirsAndFiles.Select(f => BuildLine(f)));
            var topLevelLine = $"{(char)GopherEntryType.Directory}{Constants.BbsName} Gopher Root\t/\t{Constants.Hostname}\t{_options.GopherServerPort}{Environment.NewLine}";
            response = $"{topLevelLine}{response}";

            return response;
        }

        private void WriteFileContents(string filepath, NetworkStream stream)
        {
            filepath = filepath
                .Replace("/", "\\")
                .Replace("\\Radio", "");
            if (!File.Exists(filepath))
                return;

            try
            {
                this._sessionFlags.Add(stream, new GopherServerSessionFlag
                {
                    IsDownloadingBinaryContent = true
                });

                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    do
                    {
                        var buffer = new byte[1024];
                        var bytesRead = reader.Read(buffer, 0, 1024);
                        if (bytesRead > 0)
                            stream.Write(buffer, 0, bytesRead);
                        if (bytesRead < 1024)
                            break;
                    } while (true);
                }
            }
            finally
            {
                this._sessionFlags.Remove(stream);
            }
        }

        private bool IsPublishedFile(string[] pathParts)
        {
            var link = new Models.Link
            {
                ActualFilename = string.Join("/", pathParts.Take(pathParts.Length-1)) + "/index.html",
            };
            var links = LinkParser.GetLinksFromIndex(null, link);
            var filename = pathParts.Last();
            return links.Any(x => filename.Equals(x.ActualFilename, StringComparison.CurrentCultureIgnoreCase));
        }

        private string GetRootResponse()
        {
            var rootLinks = TopLevel.GetLinks();
            var usersRoot = rootLinks.First(x => "users/".Equals(x.Path, StringComparison.CurrentCultureIgnoreCase));
            var links = new List<Models.Link>();

            // now get the user directories
            rootLinks = LinkParser.GetLinksFromIndex(null, usersRoot);
            // and for each of those look inside and see if they have a "pub" subdirectory.
            foreach (var link in rootLinks)
            {
                // don't add these
                if ("xfer/".Equals(link.Path, StringComparison.CurrentCultureIgnoreCase) ||
                    "index.html".Equals(link.ActualFilename, StringComparison.CurrentCultureIgnoreCase))
                    continue;

                if (!link.IsDirectory)
                {
                    // process non-directory in "CommunityUsers"
                    links.Add(link);
                    continue;
                }

                // process user directory
                var publicDir = LinkParser.GetLinksFromIndex(null, link)
                    .FirstOrDefault(l => "pub".Equals(l.DisplayedFilename, StringComparison.CurrentCultureIgnoreCase));
                if (publicDir != null)
                    links.Add(publicDir);
            }

            // rearrange links so that non-directories show up first, then directories
            var devNotesLink = new Models.Link
            {
                ActualFilename = "DevNotes/index.html",
                Description = "Mutiny Community Development Notes",
                DisplayedFilename = "BBS DevNotes",
                Parent = new Models.Link
                {
                    ActualFilename = "Divarin/index.html",
                    DisplayedFilename = "Divarin",
                    Parent = new Models.Link
                    {
                        ActualFilename = "users/index.html"
                    }
                }
            };
            var radioLink = new Models.Link
            {
                ActualFilename = "Radio/index.html",
                Description = "Radio Recordings",
                DisplayedFilename = "Radio",
                Parent = new Models.Link()
            };

            var files = links.Where(x => !x.IsDirectory).ToList();
            var dirs = links.Where(x => x.IsDirectory).ToList();
            links.Clear();
            links.Add(devNotesLink);
            links.Add(radioLink);
            links.AddRange(files);
            links.AddRange(dirs);

            // now that we have the links we want to show, put together a response string.
            var builder = new StringBuilder();
            foreach (var info in GetRootInfos())
            {
                builder.AppendLine(Info(info));
            }
            foreach (var link in links)
            {
                var line = BuildLine(link);
                builder.AppendLine(line);
            }
            return builder.ToString();
        }

        private string BuildLine(Models.Link link)
        {
            char c = (char)(link.IsDirectory ? GopherEntryType.Directory : GopherEntryType.TextFile);
            if (!link.IsDirectory)
            {
                var dotPos = link.ActualFilename.LastIndexOf('.');
                if (dotPos > 0 && dotPos < link.ActualFilename.Length)
                {
                    var extension = link.ActualFilename.Substring(dotPos + 1)?.ToLower().Trim();
                    if (extension != null && _gopherEntryTypeDict.ContainsKey(extension))
                    {
                        c = _gopherEntryTypeDict[extension];
                    }
                }
            }

            var owner = link.GetOwningUser();
            var displayedFilename = link.DisplayedFilename;

            if ("pub".Equals(displayedFilename, StringComparison.CurrentCultureIgnoreCase) &&
                !string.IsNullOrWhiteSpace(owner) &&
                owner.Equals(link.Parent.DisplayedFilename, StringComparison.CurrentCultureIgnoreCase))
            {
                displayedFilename = owner;
            }

            if (!string.IsNullOrWhiteSpace(link.Description))
                displayedFilename += " - " + link.Description;

            var path = link.Path;
            if (string.IsNullOrWhiteSpace(owner) && !path.StartsWith(link.Parent.Path, StringComparison.CurrentCultureIgnoreCase))
                path = link.Parent.Path + path;

            return $"{c}{displayedFilename}\t/{path}\t{Constants.Hostname}\t{_options.GopherServerPort}";
        }

        private string ReadFile(string filename)
        {
            string response = string.Empty;
            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(fs))
                response = reader.ReadToEnd();

            if (filename.EndsWith(".map", StringComparison.OrdinalIgnoreCase))
                response = ParseGopherMap(response);

            return response;
        }

        private static string Info(string message) =>
            $"{(char)GopherEntryType.Information}{message}\t\terror.host\t1";

        private IEnumerable<string> GetRootInfos()
        {
            yield return $"Welcome to the {Constants.BbsName} BBS Gopher Server!";
            yield return "This server provides public access to BBS users' text files areas.";
            yield return "";
            yield return $"{Constants.BbsName} BBS can be accessed at TELNET: {Constants.Hostname}:{_options.BbsPort}";
            yield return "";
        }

        private string ParseGopherMap(string map)
        {
            var builder = new StringBuilder();
            var lines = map.Split(new[] { (char)13 })
                .Select(x =>
                {
                    if (!string.IsNullOrWhiteSpace(x) && x[0] == 10)
                        return x.Substring(1);
                    return x;
                }).ToArray();

            var exitLoop = false;
            for (var i = 0; i < lines.Length && !exitLoop; i++)
            {
                var line = lines[i];
                var c = line.Trim().First();
                switch (c)
                {
                    case '#': continue;
                    case '.': exitLoop = true; break;
                    default:
                        if (Enum.IsDefined(typeof(GopherEntryType), (GopherEntryType)c))
                        {
                            var parts = line.Split(new[] { '\t' });
                            var selector = parts.Length >= 2 ? parts[1] : parts[0].Substring(1);
                            var host = parts.Length >= 3 ? parts[2] : Constants.Hostname;
                            var port = parts.Length >= 4 && int.TryParse(parts[3], out var p) ? p : this._options.GopherServerPort;
                            builder.AppendLine($"{c}{parts[0].Substring(1)}\t/{selector}\t{host}\t{port}");
                        }
                        break;
                }
            }

            var result = builder.ToString();
            return result;
        }
    }
}
