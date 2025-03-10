using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Services;
using miniBBS.TextFiles.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace miniBBS.TextFiles
{
    public class GopherServer : IGopherServer
    {
        const int _requestWaitSec = 30;
        private GopherServerOptions _options;
        private ILogger _logger;
        private const string _localhost = "127.0.0.1";
        
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
                    while (client.Connected && thread.ThreadState == System.Threading.ThreadState.Running && sw.Elapsed.TotalSeconds < _requestWaitSec)
                    {
                        Thread.Sleep(25);
                    }
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
                var response = $"{GetResponse(request)}{Environment.NewLine}.{Environment.NewLine}";
                var responseBytes = Encoding.ASCII.GetBytes(response);
                stream.Write(responseBytes, 0, responseBytes.Length);
                Thread.Sleep(250);
                stream.Close();
                client.Close();
            }
        }

        private string GetResponse(string selector)
        {
            if (string.IsNullOrWhiteSpace(selector) || selector.Trim() == "/")
                return GetRootResponse();

            selector = selector.Replace("\r", "").Replace("\n", "");

            var parts = selector.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1 && !"index.html".Equals(parts[0], StringComparison.CurrentCultureIgnoreCase))
            {
                // read file from users root directory
                var filename = $"{Constants.TextFileRootDirectory}users/{parts[0]}";
                if (!File.Exists(filename))
                    return Info("File not found");
                return ReadFile(filename);
            }

            if (parts.Length < 3 || !"pub".Equals(parts[2], StringComparison.CurrentCultureIgnoreCase))
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
            var files = links.Where(x => !x.IsDirectory).ToList();
            var dirs = links.Where(x => x.IsDirectory).ToList();
            links = files.Union(dirs).ToList();

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

        private static string ReadFile(string filename)
        {
            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(fs))
                return reader.ReadToEnd();
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
    }
}
