using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace miniBBS.Commands
{
    public static class Gopher
    {
        const int BufferSize = 1024 * 1024;

        public static void Execute(BbsSession session)
        {
            var previousLocation = session.CurrentLocation;
            var previousDnd = session.DoNotDisturb;
            try
            {
                session.CurrentLocation = Module.GopherSpace;
                session.DoNotDisturb = true;
                ExecuteInternal(session);
            }
            finally
            {
                session.CurrentLocation = previousLocation;
                session.DoNotDisturb = previousDnd;
            }
        }

        private static void ExecuteInternal(BbsSession session)
        {
            Stack<GopherEntry> backStack = new Stack<GopherEntry>();

            var currentLocation = new GopherEntry
            {
                Name = "Floodgap",
                Path = string.Empty,
                Host = "gopher.floodgap.com",
                Port = 70,
                EntryType = GopherEntryType.Directory,
            };

            var startBm = GopherBookmarks.GetBookmark(session, currentLocation);
            if (startBm != null)
            {
                currentLocation = ParseEntry(startBm);
            }

            var exitGopher = false;

            while (!exitGopher)
            {
                string searchTerm = null;
                if (currentLocation.EntryType == GopherEntryType.IndexSearchServer)
                {
                    session.Io.Output($"{Constants.Inverser}Enter Search Term:{Constants.Inverser} ".Color(ConsoleColor.Yellow));
                    searchTerm = session.Io.InputLine();
                    if (string.IsNullOrWhiteSpace(searchTerm))
                    {
                        if (backStack.Count > 0)
                        {
                            currentLocation = backStack.Pop();
                            continue;
                        }
                    }
                }

                session.Io.Output($"Fetching {currentLocation.Url()}...".Color(ConsoleColor.DarkGray), OutputHandlingFlag.NoWordWrap);
                var doc = GetDocument(currentLocation, searchTerm);
                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(doc))
                {
                    session.Io.Error("No content at this location.");
                    if (backStack.Count > 0)
                    {
                        currentLocation = backStack.Pop();
                        continue;
                    }
                }

                List<GopherEntry> entries = null;

                if (currentLocation.EntryType == GopherEntryType.TextFile)
                {
                    // try to fix newlines which can vary depending on source
                    doc = doc.Replace(Environment.NewLine, "\n"); // \r\n -> \n
                    doc = doc.Replace("\r", "\n"); // \r => \n
                    doc = doc.Replace("\n", session.Io.NewLine); // \n -> \r\n
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
                    {
                        session.Io.OutputLine(doc);
                    }
                }
                else
                {
                    var lines = doc.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    entries = lines
                        .Select(l => ParseEntry(l, currentLocation.Host, currentLocation.Port ?? 70))
                        .Where(e => e != null)
                        .ToList();

                    var builder = new StringBuilder();
                    var i = 1;
                    foreach (var entry in entries)
                    {
                        BuildLine(builder, ref i, entry);
                    }

                    session.Io.OutputLine(builder.ToString());

                    // from here on we only want to keep user-selectable entries
                    // the information type entries were only needed for display purposes.
                    entries = entries
                        .Where(x => x.Number.HasValue)
                        .OrderBy(x => x.Number.Value)
                        .ToList();
                }

                // prompt
                // 1234567890123456789012345678901234567890
                // Q)uit, B)ack, H)istory, G)o to, #) Go #
                var prompt =
                    $"{Constants.Inverser}gopher://{currentLocation.Host}{(currentLocation.Port == 70 ? "" : $":{currentLocation.Port}")}/{(char)currentLocation.EntryType}{currentLocation.Path}{Constants.Inverser}{session.Io.NewLine}".Color(ConsoleColor.DarkRed) +
                    $"{Constants.Inverser}K{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) + $": Bookmarks Menu{session.Io.NewLine}".Color(ConsoleColor.Cyan) +
                    $"{Constants.Inverser}Q{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) + ")uit, " +
                    $"{Constants.Inverser}B{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) + ")ack, " +
                    $"{Constants.Inverser}H{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) + ")istory, " +
                    $"{Constants.Inverser}G{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) + ")o #";
                if ( entries?.Any() == true )
                    prompt += $", {Constants.Inverser}#{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) + ") Go num";
                prompt += ": ";

                var redoMenu = false;
                do
                {
                    redoMenu = false;
                    session.Io.Output(prompt, OutputHandlingFlag.NoWordWrap);
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Green))
                    {
                        var line = session.Io.InputLine(InputHandlingFlag.ReturnFirstCharacterOnlyUnlessNumeric);
                        session.Io.OutputLine();
                        if (string.IsNullOrWhiteSpace(line))
                            break;
                        if (int.TryParse(line, out var num))
                        {
                            if (num >= 1 && num <= entries?.Count)
                            {
                                backStack.Push(currentLocation);
                                currentLocation = entries[num - 1];
                                break;
                            }
                            else
                            {
                                session.Io.Error($"Entry #{num} not found at this location.");
                                redoMenu = true;
                                break;
                            }
                        }
                        switch (char.ToUpper(line[0]))
                        {
                            case 'K':
                                {
                                    var bm = GopherBookmarks.GetBookmark(session, currentLocation);
                                    if (bm != null)
                                    {
                                        backStack.Push(currentLocation);
                                        currentLocation = ParseEntry(bm);
                                    }
                                }
                                break;
                            case 'B':
                                if (backStack.Count > 0)
                                {
                                    currentLocation = backStack.Pop();
                                }
                                else
                                {
                                    session.Io.Error("No previous location.");
                                    redoMenu = true;
                                }
                                break;
                            //case 'U':
                            //    {
                            //        var upEntry = ParseUp(currentLocation);
                            //        if (upEntry == null)
                            //        {
                            //            session.Io.Error("Can't go up from here.");
                            //            redoMenu = true;
                            //        }
                            //        backStack.Push(currentLocation);
                            //        currentLocation = upEntry;
                            //    }
                            //    break;
                            case 'H':
                                {
                                    var gotoEntry = GetHistoryItem(session, backStack);
                                    if (gotoEntry != null)
                                    {
                                        GopherEntry hist;
                                        while (backStack.Count > 0 && gotoEntry != (hist = backStack.Pop()))
                                        {
                                            // just keep pop'n
                                        }
                                        currentLocation = gotoEntry;
                                    }
                                }
                                break;
                            case 'G':
                                {
                                    var gotoEntry = GetUrl(session);
                                    if (gotoEntry != null)
                                    {
                                        backStack.Push(currentLocation);
                                        currentLocation = gotoEntry;
                                    }
                                    else
                                    {
                                        redoMenu = true;
                                    }
                                }
                                break;
                            case 'Q':
                                exitGopher = true;
                                break;
                        }
                    }
                } while (redoMenu);
            }
        }

        private static GopherEntry GetHistoryItem(BbsSession session, IEnumerable<GopherEntry> backStack)
        {
            if (backStack == null)
                backStack = new GopherEntry[0];

            int page = 0;
            const int itemsPerPage = 10; // 0-9
            session.Io.SetForeground(ConsoleColor.White);
            while (true)
            {
                var items = backStack.Skip(page * itemsPerPage).Take(itemsPerPage).ToList();

                session.Io.OutputLine($"{Constants.Inverser}History{Constants.Inverser}".Color(ConsoleColor.Yellow));

                for (int i=0; i < itemsPerPage && i < items.Count; i++)
                {
                    var item = items[i];
                    session.Io.OutputLine(
                        $"{Constants.Inverser}{i + 1}{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                        $" {item.Name}");
                }

                session.Io.OutputLine(new string('-', session.Cols - 2).Color(ConsoleColor.Yellow));

                session.Io.Output(
                    $"{Constants.Inverser}P{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    $")rev. Page   ");
                session.Io.Output(
                    $"{Constants.Inverser}N{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    $")ext Page   ");
                session.Io.OutputLine(
                    $"{Constants.Inverser}Q{Constants.Inverser}".Color(ConsoleColor.DarkMagenta) +
                    $")uit Menu");

                session.Io.Output($"{Constants.Inverser}History{Constants.Inverser}: ".Color(ConsoleColor.Yellow));
                var key = session.Io.InputKey();
                session.Io.OutputLine();
                if (key != null)
                    key = char.ToUpper(key.Value);
                else
                    return null;

                switch (key)
                {
                    case 'P':
                        if (page > 0)
                            page--;
                        break;
                    case 'N':
                        if (items.Count == itemsPerPage)
                            page++;
                        break;
                    case 'Q':
                        return null;
                    default:
                        var n = key.Value - '1';
                        if (n >= 0 && n < items.Count)
                        {
                            return items[n];
                        }
                        break;
                }
            }
        }

        private static GopherEntry GetUrl(BbsSession session)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
            {
                session.Io.Output($"{Constants.Inverser}URL{Constants.Inverser}: gopher://");
                var selector = session.Io.InputLine();
                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(selector))
                    return null;
                return ParseEntry(selector);
            }
        }

        private static GopherEntry ParseEntry(string selector)
        {
            var newEntry = new GopherEntry();

            var parts = selector.Split(new[] { '/' });
            if (parts.Length >= 1)
            {
                var host = parts[0];
                if (string.IsNullOrWhiteSpace(host))
                    return null;
                var hostAndPort = host.Split(new[] { ':' });
                if (hostAndPort.Length == 2 && int.TryParse(hostAndPort[1], out int port))
                {
                    newEntry.Host = hostAndPort[0];
                    newEntry.Port = port;
                }
                else
                {
                    newEntry.Host = host;
                    newEntry.Port = 70;
                }
            }
            
            char typeChar = (char)0;
            if (parts.Length >= 2 && parts[1].Length == 1)
                typeChar = parts[1][0];
            if (typeChar > (char)0)
                newEntry.EntryType = (GopherEntryType)typeChar;
            if (parts.Length >= 3)
                newEntry.Path = "/" + string.Join("/", parts.Skip(2));
            return newEntry;
        }

        private static GopherEntry ParseEntry(string line, string currentHost, int currentPort)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            var entry = new GopherEntry();
            int ty = line[0];
            if (!Enum.IsDefined(typeof(GopherEntryType), ty))
                return null;

            entry.EntryType = (GopherEntryType)ty;

            line = line.Substring(1);
            var parts = line.Split(new[] { '\t' }, StringSplitOptions.None);
            if (parts == null || parts.Length < 1)
                return null;

            entry.Name = parts[0];
            entry.Path = parts.Length >= 2 ? parts[1] : string.Empty;
            entry.Host = parts.Length >= 3 ? parts[2] : currentHost;
            entry.Port = parts.Length >= 4 && int.TryParse(parts[3], out int p) ? p : currentPort;

            return entry;
        }

        private static GopherEntry ParseEntry(GopherBookmark bookmark)
        {
            if (bookmark == null)
                return null;

            var url = bookmark.Selector.Replace("gopher://", "");
            var entry = ParseEntry(url);
            entry.Name = bookmark.Title;
            return entry;
        }

        private static GopherEntry ParseUp(GopherEntry currentLocation)
        {
            if (string.IsNullOrWhiteSpace(currentLocation.Path))
                return null;
            var pathParts = currentLocation.Path.Split(new[] { '/' });
            if (pathParts.Length < 1)
                return null;
            var newPath = string.Join("/", pathParts.Take(pathParts.Length - 1));
            var entry = new GopherEntry
            {
                Host = currentLocation.Host,
                Port = currentLocation.Port,
                Path = newPath,
            };
            return entry;
        }


        private static void BuildLine(StringBuilder builder, ref int index, GopherEntry entry)
        {
            var nameColor = ConsoleColor.White;

            switch (entry.EntryType)
            {
                case GopherEntryType.Directory:
                    entry.Number = index;
                    builder.Append($"{index++}: ".Color(ConsoleColor.White));
                    builder.Append("[MENU]: ".Color(ConsoleColor.Magenta));
                    nameColor = ConsoleColor.Cyan;
                    break;
                case GopherEntryType.TextFile:
                    entry.Number = index;
                    builder.Append($"{index++}: ".Color(ConsoleColor.White));
                    nameColor = ConsoleColor.Cyan;
                    builder.Append("[TEXT]: ".Color(ConsoleColor.Green));
                    break;
                case GopherEntryType.IndexSearchServer:
                    entry.Number = index;
                    builder.Append($"{index++}: ".Color(ConsoleColor.White));
                    nameColor = ConsoleColor.Cyan;
                    builder.Append("[SRCH]: ".Color(ConsoleColor.Blue));
                    break;
                case GopherEntryType.Information:
                    nameColor = ConsoleColor.DarkGreen;
                    break;
                case GopherEntryType.Error:
                    builder.Append("[ERR!]: ".Color(ConsoleColor.Red));
                    nameColor = ConsoleColor.Red;
                    break;
                default:
                    builder.Append("[????]: ".Color(ConsoleColor.DarkRed));
                    nameColor = ConsoleColor.DarkCyan;
                    break;
            }
           
            builder.Append(entry.Name.Color(nameColor));
            if (!string.IsNullOrWhiteSpace(entry.Description))
                builder.Append($"\r\n{entry.Description}".Color(ConsoleColor.Gray));
            builder.AppendLine();
        }

        private static string GetDocument(GopherEntry entry, string searchTerm)
        {
            var builder = new StringBuilder();

            try
            {
                using (var client = new TcpClient(entry.Host, entry.Port ?? 70))
                using (var stream = client.GetStream())
                {
                    if (entry.Path == null)
                        entry.Path = string.Empty;

                    if (entry.Path.StartsWith("//"))
                        entry.Path = entry.Path.Substring(1); // remove double-slash if it exists

                    var requestPath = entry.Path;
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                        requestPath += $"\t{searchTerm}";
                    requestPath += Environment.NewLine;

                    var request = Encoding.ASCII.GetBytes(requestPath);
                    stream.Write(request, 0, request.Length);
                    
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    do
                    {
                        byte[] buffer = new byte[BufferSize];
                        var bytesRead = stream.Read(buffer, 0, BufferSize);
                        if (bytesRead > 0)
                        {
                            var text = Encoding.ASCII.GetString(buffer.Where(c => c > 0).ToArray());
                            builder.Append(text);
                        }
                        else
                        {
                            break;
                        }
                    } while (stopwatch.Elapsed.TotalSeconds < 15);
                }
                return builder.ToString();
            } catch (SocketException ex)
            {
                return $"3{ex.Message}{Environment.NewLine}";
            }
        }
    }
}
