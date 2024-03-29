﻿using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class Bulletins
    {
        private const string END_QUOTE = " --- End Quote ---";
        
        //private const string NO_MORE_UNREAD = 
        //    "No more unread messages in the current board use ']' to advance to the next.   " + 
        //    "Also try going to the chat rooms and    " + 
        //    "reading the backlog there!";
        //   1234567890123456789012345678901234567890
        public static void Execute(BbsSession session)
        {
            var previousLocation = session.CurrentLocation;
            session.CurrentLocation = Module.BulletinBoard;
            bool wasDnd = session.DoNotDisturb;
            session.DoNotDisturb = true;

            var exitMenu = false;

            var boardRepo = DI.GetRepository<BulletinBoard>();
            var bulletinRepo = DI.GetRepository<Bulletin>();
            var metaRepo = DI.GetRepository<Metadata>();
            var meta = metaRepo.Get(new Dictionary<string, object>
            {
                {nameof(Metadata.UserId), session.User.Id},
                {nameof(Metadata.Type), MetadataType.ReadBulletins}
            })?.PruneAllButMostRecent(metaRepo);

            var readBulletins = meta != null ?
                JsonConvert.DeserializeObject<List<int>>(meta.Data) :
                new List<int>();

            var boards = boardRepo.Get()
                .OrderBy(b => b.Id)
                .ToList();

            var currentBoard = boards.First();

            Dictionary<int, Bulletin> bulletins = null;

            void ReloadBulletins()
            {
                bulletins = bulletinRepo
                    .Get(x => x.BoardId, currentBoard.Id)
                    .OrderBy(b => b.Id)
                    .ToDictionary(k => k.Id);
            }

            int? lastRead = null;
            void NextUnread()
            {
                Notice(session, "Finding next unread message");
                var n = bulletins.Keys.FirstOrDefault(x => !readBulletins.Contains(x));
                if (n > 0)
                {
                    ReadBulletin(session, bulletins, n, readBulletins);
                    lastRead = n;
                }
                else
                {
                    // no new unread in current board
                    // try advance to next board
                    session.Io.OutputLine($"No more unread messages in '{currentBoard.Name}'.".Color(ConsoleColor.Magenta));
                    var nextBoard = boards.FirstOrDefault(x => x.Id > currentBoard.Id);
                    if (nextBoard != null)
                    {
                        session.Io.OutputLine($"Going to '{nextBoard.Name}' board.".Color(ConsoleColor.Magenta));
                        currentBoard = nextBoard;
                        ReloadBulletins();
                        n = bulletins.Keys.FirstOrDefault(x => !readBulletins.Contains(x));
                    }
                    if (n > 0)
                    {
                        ReadBulletin(session, bulletins, n, readBulletins);
                        lastRead = n;
                    }
                    else
                        session.Io.Error("No more unread messages.  Try reading the backlog in the Chat rooms!");
                }
            }

            ReloadBulletins();

            ShowMenu(session);
            try
            {
                do
                {
                    session.Io.Output($"{Constants.Inverser}[Bulletins:{currentBoard.Name.Color(ConsoleColor.Yellow)}] {"(?=Help)".Color(ConsoleColor.DarkGray)} >{Constants.Inverser} ".Color(ConsoleColor.White));
                    var key = session.Io.InputKey();

                    if (!key.HasValue || key == '\r' || key == '\n' || $"{key}" == session.Io.NewLine)
                        key = 'N';
                    
                    session.Io.Output(key.Value);
                    key = char.ToUpper(key.Value);

                    int jumpToMessageNumber = -1;
                    if (key.HasValue && char.IsDigit(key.Value))
                    {
                        var restOfTheNumber = session.Io.InputLine();
                        if (string.IsNullOrWhiteSpace(restOfTheNumber))
                        {
                            jumpToMessageNumber = int.Parse($"{key.Value}");
                        }
                        else if (int.TryParse(restOfTheNumber, out int n1))
                        {
                            jumpToMessageNumber = int.Parse($"{key.Value}{n1}");
                        }
                    }

                    session.Io.OutputLine();
                    switch (key)
                    {
                        case '[':
                        case '{':
                            currentBoard = boards.LastOrDefault(x => x.Id < currentBoard.Id);
                            if (currentBoard == null)
                                currentBoard = boards.Last();
                            ReloadBulletins();
                            // previous board
                            break;
                        case ']':
                        case '}':
                            currentBoard = boards.FirstOrDefault(x => x.Id > currentBoard.Id);
                            if (currentBoard == null)
                                currentBoard = boards.First();
                            ReloadBulletins();
                            // next board
                            break;
                        case '_':
                        case '-':
                            // previous message (don't follow thread)
                            {
                                var n =
                                    lastRead.HasValue ?
                                    bulletins.Keys.LastOrDefault(x => x < lastRead.Value) :
                                    bulletins.Keys.FirstOrDefault();
                                ReadBulletin(session, bulletins, n, readBulletins);
                                lastRead = n;
                            }
                            break;
                        case '=':
                        case '+':
                            // next message (don't follow thread)
                            {
                                var n = lastRead.HasValue ?
                                    bulletins.Keys.FirstOrDefault(x => x > lastRead.Value) :
                                    bulletins.Keys.FirstOrDefault();
                                ReadBulletin(session, bulletins, n, readBulletins);
                                lastRead = n;
                            }
                            break;
                        case 'N':
                            // next unread
                            NextUnread();
                            break;
                        case '#':
                            session.Io.Error("'#' means:  Enter the bulletin number to read it.");
                            break;
                        case '<':
                        case ',':
                            // previous bulletin
                            if (!bulletins.Any())
                            {
                                session.Io.Error("No messages, try (P)osting one!");
                                break;
                            }
                            if (lastRead.HasValue && lastRead.Value <= bulletins.Keys.Min())
                            {
                                session.Io.Error("No earlier messages.");
                            }
                            else
                            {
                                Notice(session, "Finding previous in thread");
                                int? n = TryFindPrevInThread(lastRead, bulletins);
                                if (!n.HasValue)
                                {
                                    Notice(session, "Not found, finding next older message");
                                    n = lastRead.HasValue ?
                                        bulletins.Keys.LastOrDefault(x => x < lastRead.Value) :
                                        bulletins.Keys.FirstOrDefault();
                                }
                                ReadBulletin(session, bulletins, n.Value, readBulletins);
                                lastRead = n;
                            }
                            break;
                        case '>':
                        case '.':
                            // next bulletin
                            if (lastRead.HasValue)
                            {
                                Notice(session, "Finding next in thread");
                                var n = TryFindNextInThread(lastRead, bulletins);
                                if (n.HasValue)
                                {
                                    ReadBulletin(session, bulletins, n.Value, readBulletins);
                                    lastRead = n;
                                    break;
                                }
                                Notice(session, "No more in thread");
                            }
                            if (!lastRead.HasValue)
                            {
                                var n = bulletins.Keys.FirstOrDefault();
                                if (n > 0)
                                {
                                    ReadBulletin(session, bulletins, n, readBulletins);
                                    lastRead = n;
                                }
                                else
                                    Notice(session, "No bulletins on this board, use ']' to advance to next.");
                            }
                            else
                            {
                                var n = bulletins.Keys.FirstOrDefault(x => x > lastRead.Value);
                                if (n > 0)
                                {
                                    ReadBulletin(session, bulletins, n, readBulletins);
                                    lastRead = n;
                                }
                                else
                                    NextUnread();
                            }
                            break;
                        case 'B':
                            // back one subject
                            {
                                Notice(session, "Finding start of previous thread");
                                var n =
                                    lastRead.HasValue ?
                                    bulletins.LastOrDefault(x => x.Key < lastRead.Value && x.Value.ResponseToId == null).Key :
                                    bulletins.Keys.FirstOrDefault();
                                ReadBulletin(session, bulletins, n, readBulletins);
                                lastRead = n;
                            }
                            break;
                        case 'F':
                            // forward one subject
                            {
                                Notice(session, "Finding start of next thread");
                                var n =
                                    lastRead.HasValue ?
                                    bulletins.FirstOrDefault(x => x.Key > lastRead.Value && x.Value.ResponseToId == null).Key :
                                    bulletins.Keys.FirstOrDefault();
                                ReadBulletin(session, bulletins, n, readBulletins);
                                lastRead = n;
                            }
                            break;
                        case 'R':
                            // reply
                            if (!lastRead.HasValue)
                                session.Io.Error("Reply to what?  You haven't read a message yet!");
                            else if (bulletins.TryGetValue(lastRead.Value, out var original))
                            {
                                if (Reply(session, currentBoard, bulletinRepo, original))
                                    ReloadBulletins();
                            }
                            else
                                session.Io.Error("I can't find the message you're replying to!");
                            break;
                        case 'P':
                            // post
                            if (PostMessage(session, currentBoard, bulletinRepo, readBulletins))
                                ReloadBulletins();
                            break;
                        case 'L':
                            // list
                            ListBulletins(session, bulletins, readBulletins);
                            break;
                        case 'E':
                        case 'D':
                            // edit / delete
                            if (EditBulletin(session, currentBoard, bulletinRepo, bulletins, delete: key == 'D'))
                                ReloadBulletins();
                            break;
                        case 'Q':
                            // quit
                            exitMenu = true;
                            break;
                        case '?':
                            ShowMenu(session);
                            break;
                        default:
                            if (jumpToMessageNumber > -1)
                            {
                                // jump to message by number
                                Notice(session, $"Jumping to message # {jumpToMessageNumber}");
                                ReadBulletin(session, bulletins, jumpToMessageNumber, readBulletins);
                                lastRead = jumpToMessageNumber;
                            }
                            else
                                session.Io.Error("Unrecognized command, press question mark (?) for help.");
                            break;
                    }
                } while (!exitMenu);
            } 
            finally
            {
                if (meta == null)
                {
                    meta = new Metadata
                    {
                        UserId = session.User.Id,
                        Type = MetadataType.ReadBulletins
                    };
                }
                meta.Data = JsonConvert.SerializeObject(readBulletins);
                metaRepo.InsertOrUpdate(meta);
                session.CurrentLocation = previousLocation; 
                session.DoNotDisturb = wasDnd;
            }
        }

        private static bool EditBulletin(BbsSession session, BulletinBoard board, IRepository<Bulletin> repo, Dictionary<int, Bulletin> bulletins, bool delete)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.Output($"{(delete ? "Delete" : "Edit")} bulletin #: ");
                var line = session.Io.InputLine();
                session.Io.OutputLine();
                if (!int.TryParse(line, out var num) || !bulletins.ContainsKey(num))
                    return false;
                var bulletin = bulletins[num];
                if (bulletin.FromUserId != session.User.Id)
                {
                    session.Io.Error("Not your bulletin!");
                    return false;
                }

                // in market can edit post no matter how old as long as it's yours
                if (delete || !"market".Equals(board.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    var isModerator = session.User.Access.HasFlag(AccessFlag.Administrator) || session.User.Access.HasFlag(AccessFlag.GlobalModerator);
                    var hasResponses = bulletins.Values.Any(b => b.OriginalId == bulletin.Id);
                    var tooOld = (DateTime.UtcNow - bulletin.DateUtc).TotalMinutes > Constants.MinutesUntilMessageIsUndeletable;

                    if (!isModerator && (tooOld || hasResponses))
                    {
                        session.Io.Error($"Bulletin too old to be {(delete ? "deleted" : "edited")}.");
                        return false;
                    }
                }

                if (delete)
                {
                    repo.Delete(bulletin);
                    bulletins.Remove(bulletin.Id);
                    return true;
                }
                else
                {
                    session.Io.OutputLine($"Current subject: {bulletin.Subject}");
                    if ('Y' == session.Io.Ask("Change subject"))
                    {
                        session.Io.Output("New Subject: ");
                        var newSubject = session.Io.InputLine();
                        if (!string.IsNullOrWhiteSpace(newSubject))
                            bulletin.Subject = newSubject;
                    }
                    var editor = DI.Get<ITextEditor>();
                    editor.OnSave = body =>
                    {
                        bulletin.Message = body;
                        repo.Update(bulletin);
                        session.Io.Error("Bulletin edited");
                        return string.Empty;
                    };
                    editor.EditText(session, new LineEditorParameters
                    {
                        PreloadedBody = bulletin.Message,
                        QuitOnSave = true
                    });
                    return false;
                }
            }
        }

        private static void Notice(BbsSession session, string notice)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.DarkGray))
            {
                session.Io.OutputLine(notice);
            }
        }

        private static int? TryFindNextInThread(int? lastRead, Dictionary<int, Bulletin> bulletins)
        {
            if (!lastRead.HasValue)
                return null;

            var currentId = lastRead.Value;
            if (!bulletins.TryGetValue(currentId, out var current))
                return null;

            var next = bulletins.FirstOrDefault(b =>
                b.Key > currentId && 
                b.Value != null &&
                ((b.Value.OriginalId ?? b.Key) == (current.OriginalId ?? currentId)))
                .Value;

            if (next != null)
                return next.Id;

            return null;
        }

        private static int? TryFindPrevInThread(int? lastRead, Dictionary<int, Bulletin> bulletins)
        {
            if (!lastRead.HasValue)
                return null;

            var currentId = lastRead.Value;
            if (!bulletins.TryGetValue(currentId, out var current) || current.OriginalId == null)
                return null;

            var prev = bulletins.LastOrDefault(b => 
                b.Key < currentId && 
                b.Value != null &&
                (b.Value.OriginalId ?? b.Key) == current.OriginalId)
                .Value;

            if (prev != null)
                return prev.Id;

            return null;
        }

        private static bool Reply(BbsSession session, BulletinBoard board, IRepository<Bulletin> repo, Bulletin originalMessage)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                var didPost = false;

                if (!session.Usernames.TryGetValue(originalMessage.FromUserId, out var originalUsername))
                    originalUsername = "Unknown";

                session.Io.OutputLine("Replying to " + originalUsername.Color(ConsoleColor.Green) + "'s post: " + originalMessage.Subject.Color(ConsoleColor.Yellow));

                var quote = string.Empty;
                var k = session.Io.Ask($"Quote (S)ome, (A)ll, or (N)one of the {originalUsername}'s message?");
                if (k == 'A')
                    quote = RemoveQuotes(originalMessage.Message);
                else if (k == 'S')
                    quote = GetPartialQuote(session, originalMessage);

                if (!string.IsNullOrWhiteSpace(quote))
                {
                    quote = string.Join(Environment.NewLine, new[]
                    {
                        $" --- Quote Msg#{originalMessage.Id} from {originalUsername} ---".Color(ConsoleColor.DarkGray),
                        quote.Color(ConsoleColor.Blue),
                        END_QUOTE.Color(ConsoleColor.DarkGray),
                    });
                }

                var subject = originalMessage.Subject;
                if (!subject.StartsWith("re: "))
                    subject = $"re: {subject}";

                session.Io.OutputLine($"Subject: {subject}");
                if (session.Io.Ask("Change subject?") == 'Y')
                {
                    session.Io.Output("Subject: ");
                    var newSubject = session.Io.InputLine(InputHandlingFlag.MaxLength);
                    session.Io.OutputLine();
                    if (!string.IsNullOrWhiteSpace(newSubject))
                        subject = newSubject;
                }

                session.Io.OutputLine(quote);

                // POST REPLY
                var editor = DI.Get<ITextEditor>();
                editor.OnSave = body =>
                {
                    if (!string.IsNullOrWhiteSpace(quote))
                        body = $"{quote}{Environment.NewLine}{body}";
                    var posted = repo.Insert(new Bulletin
                    {
                        BoardId = board.Id,
                        FromUserId = session.User.Id,
                        ToUserId = originalMessage.FromUserId,
                        DateUtc = DateTime.UtcNow,
                        ResponseToId = originalMessage.Id,
                        OriginalId = originalMessage.OriginalId ?? originalMessage.Id,
                        Subject = subject,
                        Message = body
                    });
                    //readBulletins.Add(posted.Id);
                    didPost = true;
                    DI.Get<IMessager>().Publish(session, new GlobalMessage(
                        session.Id,
                        $"{session.User.Name} has replied to a Bulletin in {board.Name}, #{posted.Id}, Subject: {subject.Color(ConsoleColor.Green)}.  Use '/b' to go to the Bulletin Boards."));
                    return string.Empty;
                };

                editor.EditText(session, new LineEditorParameters
                {
                    QuitOnSave = true
                });

                if (!didPost)
                    session.Io.Error(" *** Post Aborted! ***");

                return didPost;
            }
        }

        private static string RemoveQuotes(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return string.Empty;

            var pos = message.IndexOf(END_QUOTE);
            if (pos <= 0)
                return message;

            pos = message.IndexOf(Environment.NewLine, pos);
            pos += Environment.NewLine.Length;
            message = message.Substring(pos);
            return message;
        }

        private static string GetPartialQuote(BbsSession session, Bulletin originalMessage)
        {
            var split = originalMessage
                .Message
                .SplitAndWrap(session, OutputHandlingFlag.None)
                .Select(x => x.Replace("\r", "").Replace("\n", ""))
                .ToList();

            while (true)
            {
                session.Io.Output($"{Constants.Inverser}Enter range (#-#) or (L)ist:{Constants.Inverser} ".Color(ConsoleColor.DarkCyan));
                var inp = session.Io.InputLine();
                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(inp))
                    return null;
                if ("L".Equals(inp, StringComparison.CurrentCultureIgnoreCase))
                {
                    int l = 1;
                    session.Io.OutputLine(string.Join(Environment.NewLine, split
                        .Select(x => $"{l++}: {x}")));
                }
                else if (int.TryParse(inp, out var n))
                {
                    n--;
                    if (n < 0 || n >= split.Count)
                        session.Io.Error("Invalid range");
                    else
                        return split[n];
                }
                else if (inp.Contains("-"))
                {
                    var range = inp.Split('-');
                    if (range.Length == 2 && int.TryParse(range[0], out var start) && int.TryParse(range[1], out var end))
                    {
                        start = Math.Min(start, end) - 1;
                        end = Math.Max(start, end) - 1;
                        
                        if (start < 0 || end >= split.Count)
                            session.Io.Error("Invalid range");
                        else
                        {
                            var builder = new StringBuilder();
                            for (int i=start;  i <= end; i++)
                            {
                                if (i == end && string.IsNullOrWhiteSpace(split[i]))
                                    break;
                                builder.AppendLine(split[i]);
                            }
                            return builder.ToString();
                        }
                    }
                }
            }
        }

        private static void ReadBulletin(BbsSession session, Dictionary<int, Bulletin> bulletins, int bulletinId, List<int> readBulletins)
        {
            if (bulletinId < bulletins.Keys.Min())
                bulletinId = bulletins.Keys.Min();

            var found = bulletins.TryGetValue(bulletinId, out var bulletin);
            if (!found)
            {
                session.Io.Error($"Unable to find Bulletin #{bulletinId}!");
                return;
            }

            if (!readBulletins.Contains(bulletinId))
                readBulletins.Add(bulletinId);

            var builder = new StringBuilder();

            // format header
            var reNum = bulletin.ResponseToId?.ToString() ?? "None";
            if (!session.Usernames.TryGetValue(bulletin.FromUserId, out var fromUsername))
                fromUsername = "Unknown";
            var toUsername = "All";
            if (bulletin.ToUserId.HasValue)
            {
                if (!session.Usernames.TryGetValue(bulletin.ToUserId.Value, out toUsername))
                    toUsername = "Unknown";
            }
            builder.AppendLine(string.Join("", new[]
                {
                    "Msg #: ".Color(ConsoleColor.Cyan),
                    bulletinId.ToString().PadRight(12).Color(ConsoleColor.White),
                    "Re   : ".Color(ConsoleColor.Cyan),
                    reNum.Color(ConsoleColor.DarkGray),
                    Environment.NewLine,
                    "From : ".Color(ConsoleColor.Cyan),
                    fromUsername.PadRight(12).Color(ConsoleColor.Yellow),
                    "To   : ".Color(ConsoleColor.Cyan),
                    toUsername.Color(ConsoleColor.Yellow),
                    Environment.NewLine,
                    "Date : ".Color(ConsoleColor.Cyan),
                    $"{bulletin.DateUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm}".Color(ConsoleColor.Blue),
                    Environment.NewLine,
                    "Subj : ".Color(ConsoleColor.Cyan),
                    bulletin.Subject.Color(ConsoleColor.Yellow),
                    Environment.NewLine,
                    $"{Constants.Spaceholder}---------- ".Color(ConsoleColor.Green)
                }));

            // account for messages that contain the old colorizer character
            // this char was changed due to conflicts with an atascii character.
            var body = bulletin.Message
                .Replace('½', Constants.InlineColorizer);

            // add body
            builder.Append(body);

            // show message
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Green))
            {
                session.Io.OutputLine(builder.ToString());
            }
        }

        private static void ListBulletins(BbsSession session, Dictionary<int, Bulletin> bulletins, List<int> readBulletins)
        {
            const int usernameLength = 8;

            if (true != bulletins?.Any())
            {
                session.Io.Error("No bulletins! Try (P)osting one!");
                return;
            }

            List<Bulletin> toList = null;
            while (toList == null)
            {
                session.Io.OutputLine($"Range: {bulletins.Keys.Min()}-{bulletins.Keys.Max()}");
                var option = session.Io.AskWithNumber("U)nread, #) From #, ENTER=All");
                if (option == "U")
                {
                    toList = bulletins.Values
                        .Where(b => !readBulletins.Contains(b.Id))
                        .OrderBy(b => b.DateUtc)
                        .ToList();
                }
                else if (option == "#")
                {
                    session.Io.Error("By '#' I meant, type in a number.");
                }
                else if (!string.IsNullOrWhiteSpace(option) && int.TryParse(option, out var startAt))
                {
                    toList = bulletins.Values
                        .Where(b => b.Id >= startAt)
                        .OrderBy(b => b.DateUtc)
                        .ToList();
                }
                else
                {
                    toList = bulletins.Values
                        .OrderBy(b => b.DateUtc)
                        .ToList();
                }
            };

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                var builder = new StringBuilder();

                session.Io.OutputLine($"{Constants.Inverser}*# Date. From.... Subject{Constants.Inverser}".Replace('.', Constants.Spaceholder));

                session.Io.OutputLine("--------------------------------------");
                foreach (var bull in toList)
                {
                    var isRead = true == readBulletins?.Contains(bull.Id) ? $"{Constants.Spaceholder}" : "*".Color(ConsoleColor.Red);
                    var printableMsgNum = bull.Id.ToString();
                    var msgNum = printableMsgNum
                        //.PadLeft(4, Constants.Spaceholder)
                        .Color(ConsoleColor.White);
                    var printablePostedDate = bull.DateUtc.AddHours(session.TimeZone).ToString("MM/dd");
                    var postedDate = printablePostedDate.Color(ConsoleColor.DarkGray);

                    var printableFromUserName = session.Usernames.ContainsKey(bull.FromUserId) ? session.Usernames[bull.FromUserId] : "Unknown";
                    if (printableFromUserName.Length > usernameLength)
                        printableFromUserName = printableFromUserName.Substring(0, usernameLength);
                    else if (printableFromUserName.Length < usernameLength)
                        printableFromUserName = printableFromUserName.PadRight(usernameLength, Constants.Spaceholder);
                    var fromUserName = printableFromUserName.Color(ConsoleColor.Yellow);

                    //var toUserName = bull.ToUserId.HasValue ?
                    //    (session.Usernames.ContainsKey(bull.ToUserId.Value) ? session.Usernames[bull.ToUserId.Value] : "Unknown") :
                    //    "All";
                    //if (toUserName.Length > usernameLength)
                    //    toUserName = toUserName.Substring(0, usernameLength);
                    //else if (toUserName.Length < 8)
                    //    toUserName = toUserName.PadRight(usernameLength, Constants.Spaceholder);
                    //toUserName = toUserName.Color(ConsoleColor.Yellow);

                    var printableSubject = bull.Subject;
                    
                    var printable = $"*{printableMsgNum} {printablePostedDate} {printableFromUserName} {printableSubject}";
                    var printableLength = printable.Length;
                    var maxLength = session.Cols - 3;
                    if (printableLength >= maxLength)
                    {
                        var change = printableLength - maxLength;
                        var subjectLength = printableSubject.Length - change;
                        if (subjectLength > 0)
                            printableSubject = printableSubject.Substring(0, subjectLength);
                    }

                    var subject = printableSubject.Color(ConsoleColor.DarkGreen);
                    builder.AppendLine($"{isRead}{msgNum} {postedDate} {fromUserName} {subject}");
                    //builder.AppendLine($"{isRead}{bull.Id.ToString().Color(ConsoleColor.White)} {postedDate}  {fromUserName.UniqueColor()}  {toUserName.UniqueColor()}  {bull.Subject.Color(ConsoleColor.DarkGreen)}");
                }
                session.Io.OutputLine(builder.ToString());
            }
        }

        private static void ShowMenu(BbsSession session)
        {
            // 1234567890123456789012345678901234567890
            // <=Prev. in Thread    >/CR=Next in Thread
            var eq = "=".Color(ConsoleColor.DarkGray);
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
            {
                session.Io.OutputLine($"{Constants.Inverser} *** Community Bulletins Menu ***{Constants.Inverser}\r\n".Color(ConsoleColor.DarkMagenta));
                session.Io.OutputLine(
                    "[".Color(ConsoleColor.Green) + eq + "Prev Board         " +
                    "]".Color(ConsoleColor.Green) + eq + "Next Board");
                session.Io.OutputLine(
                    "N".Color(ConsoleColor.Green) + eq + "Next Unread        " +
                    "#".Color(ConsoleColor.Green) + eq + "Jump to #");
                session.Io.OutputLine("Follow thread with < and >:".Color(ConsoleColor.DarkCyan));
                session.Io.OutputLine(
                    "<".Color(ConsoleColor.Green) + eq + "Prev. in Thread    " +
                    ">".Color(ConsoleColor.Green) + eq + "Next in Thread");
                session.Io.OutputLine("Don't follow thread with - and +:".Color(ConsoleColor.DarkCyan));
                session.Io.OutputLine(
                    "-".Color(ConsoleColor.Green) + eq + "Prev. Bulletin     " +
                    "+".Color(ConsoleColor.Green) + eq + "Next Bulletin");
                session.Io.OutputLine(
                    "B".Color(ConsoleColor.Green) + eq + "Back One Subject   " +
                    "F".Color(ConsoleColor.Green) + eq + "Fwd. One Subject");
                session.Io.OutputLine(
                    "R".Color(ConsoleColor.Green) + eq + "Reply to Bulletin  " +
                    "P".Color(ConsoleColor.Green) + eq + "Post New");
                session.Io.OutputLine(
                    "L".Color(ConsoleColor.Green) + eq + "List Bulletins     " +
                    "Q".Color(ConsoleColor.Green) + eq + "Quit Bulletins");
                session.Io.OutputLine("-------------------------------------".Color(ConsoleColor.DarkGray));
                session.Io.OutputLine(
                    "E".Color(ConsoleColor.Green) + eq + "Edit a Bulletin    " +
                    "D".Color(ConsoleColor.Green) + eq + "Del. a Bulletin");
            }
        }

        internal static Count Count(BbsSession session)
        {
            var metaRepo = DI.GetRepository<Metadata>();

            var meta = metaRepo.Get(new Dictionary<string, object>
            {
                {nameof(Metadata.UserId), session.User.Id},
                {nameof(Metadata.Type), MetadataType.ReadBulletins}
            })?.PruneAllButMostRecent(metaRepo);

            var readBulletins = meta != null ?
                JsonConvert.DeserializeObject<List<int>>(meta.Data) :
                new List<int>();

            var bulletinRepo = DI.GetRepository<Bulletin>();
            var bulletins = bulletinRepo.Get().ToList();

            return new Count
            {
                TotalCount = bulletins?.Count() ?? 0,
                SubsetCount = bulletins?.Count(b => !readBulletins.Contains(b.Id)) ?? 0
            };
        }

        private static bool PostMessage(BbsSession session, BulletinBoard board, IRepository<Bulletin> repo, List<int> readBulletins)
        {
            var didPost = false;

            string subject;
            int? toUserId = null;
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine($"{Constants.Spaceholder}{Constants.Inverser} --- Posting a new Bulletin ---{Constants.Inverser}");
                toUserId = FindToUserId(session);
                session.Io.Output($"{Constants.Inverser}Subject:{Constants.Inverser} ");
                subject = session.Io.InputLine(InputHandlingFlag.MaxLength);
                session.Io.OutputLine();
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                session.Io.Error(" *** Post Aborted! ***");
                return false;
            }

            var editor = DI.Get<ITextEditor>();
            editor.OnSave = body =>
            {
                var posted = repo.Insert(new Bulletin
                {
                    BoardId = board.Id,
                    FromUserId = session.User.Id,
                    ToUserId = toUserId,
                    DateUtc = DateTime.UtcNow,
                    Subject = subject,
                    Message = body
                });
                didPost = true;
                DI.Get<IMessager>().Publish(session, new GlobalMessage(
                    session.Id,
                    $"{session.User.Name} has posted a new Bulletin in {board.Name}, #{posted.Id}, Subject: {subject.Color(ConsoleColor.Green)}."));
                return string.Empty;
            };

            editor.EditText(session, new LineEditorParameters
            {
                QuitOnSave = true
            });

            if (!didPost)
                session.Io.Error(" *** Post Aborted! ***");

            return didPost;
        }

        private static int? FindToUserId(BbsSession session)
        {
            do
            {
                session.Io.Output($"{Constants.Inverser}To (enter=All):{Constants.Inverser} ");
                var toUsername = session.Io.InputLine(InputHandlingFlag.MaxLength);
                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(toUsername))
                    return null;
                {
                    var to = session.Usernames.FirstOrDefault(x => toUsername.Trim().Equals(x.Value.Trim(), StringComparison.CurrentCultureIgnoreCase));
                    if (!string.IsNullOrWhiteSpace(to.Value))
                        return to.Key;
                    else
                        session.Io.Error("Unknown user");
                }
            } while (true);
        }
    }
}
