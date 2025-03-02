using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class Seen
    {
        //                                  1234567890123456789012345678901234567890
        private const string _seenHeader = "Who       When               Bye";

        //                                      12345678901234567890123456789012345678901234567890123456789012345678901234567890
        private const string _seenHeaderWide = "Who                           When                  Bye";

        public static void Execute(BbsSession session, params string[] args)
        {
            var repo = DI.GetRepository<Metadata>();
            if (true != args?.Any())
                ShowLastSeenUser(session, repo);
            else if (args.Length == 1 && int.TryParse(args[0], out int count) && count > 0 && count <= 100)
                ShowLastNUsers(session, repo, count);
            else
                ShowUsers(session, repo, args);
        }

        private static void ShowLastNUsers(BbsSession session, IRepository<Metadata> repo, int count)
        {
            var last = repo.Get(x => x.Type, MetadataType.SeenData)
                ?.Where(x => x.DateAddedUtc.HasValue)
                ?.OrderByDescending(x => x.DateAddedUtc.Value)
                ?.Take(count);

            var data = last
                .Select(m => new
                {
                    UserId = m.UserId.Value,
                    Data = JsonConvert.DeserializeObject<SeenData>(m.Data)
                });

            var builder = new StringBuilder();
            var header = session.Cols >= 80 ? _seenHeaderWide : _seenHeader;
            builder.AppendLine($"{Constants.Inverser}{header}{Constants.Inverser}".Color(ConsoleColor.Magenta));

            foreach (var d in data)
                builder.AppendLine(GetLine(session, d.Data, d.UserId));

            session.Io.Output(builder.ToString());
        }

        private static void ShowLastSeenUser(BbsSession session, IRepository<Metadata> repo)
        {
            var last = repo.Get(x => x.Type, MetadataType.SeenData)
                ?.Where(x => x.DateAddedUtc.HasValue)
                ?.OrderByDescending(x => x.DateAddedUtc.Value)
                ?.FirstOrDefault();

            if (last?.UserId != null)
            {
                var data = JsonConvert.DeserializeObject<SeenData>(last.Data);
                session.Io.OutputLine(_seenHeader.Color(ConsoleColor.Magenta));
                session.Io.OutputLine(GetLine(session, data, last.UserId.Value));
            }
            else
                session.Io.Error("I don't remember the last time someone was on here.");
        }

        private static void ShowUsers(BbsSession session, IRepository<Metadata> repo, string[] usernames)
        {
            var userIds = session.Usernames
                .Where(x => usernames.Contains(x.Value, StringComparer.CurrentCultureIgnoreCase))
                .Select(x => x.Key)
                .ToArray();

            var reverseDict = session.Usernames
                .ToDictionary(k => k.Value, v => v.Key, StringComparer.CurrentCultureIgnoreCase);

            var seens = repo.Get(x => x.Type, MetadataType.SeenData)
                ?.Where(x => x.UserId.HasValue && userIds.Contains(x.UserId.Value))
                ?.ToDictionary(k => k.UserId);

            var builder = new StringBuilder();
            builder.AppendLine(_seenHeader.Color(ConsoleColor.Magenta));

            foreach (var un in usernames)
            {
                if (reverseDict.TryGetValue(un, out int userId) && seens.ContainsKey(userId))
                {
                    var seen = seens[userId];
                    var data = JsonConvert.DeserializeObject<SeenData>(seen.Data);
                    builder.AppendLine(GetLine(session, data, seen.UserId.Value));
                }
                else
                    session.Io.Error($"I don't remember the last time {un} was on here.");
            }

            session.Io.Output(builder.ToString());
        }

        private static string GetLine(BbsSession session, SeenData seen, int userId)
        {
            var username = session.Usernames.ContainsKey(userId) ? session.Usernames[userId] : "Unknown";
            string login;
            if (session.Cols >= 80)
            {
                login = seen.SessionsStartUtc.Year == DateTime.Now.Year ?
                    $"{seen.SessionsStartUtc.AddHours(session.TimeZone):MMM dd HH:mm}   " :
                    $"{seen.SessionsStartUtc.AddHours(session.TimeZone):yy MMM dd HH:mm}";
            }
            else
            {
                login = seen.SessionsStartUtc.Year == DateTime.Now.Year ?
                    $"{seen.SessionsStartUtc.AddHours(session.TimeZone):MMM dd HH:mm}" :
                    $"{seen.SessionsStartUtc.AddHours(session.TimeZone):yyMMdd HH:mm}";
            }

            var nameLengh = 7; // 10 - 3 for elipsis (...)
            if (session.Cols >= 80)
                nameLengh += 20; // can afford to show more of the username

            return
                $"{Constants.Inverser}{username.MaxLength(nameLengh).PadRight(nameLengh + 3)}{Constants.Inverser}".Color(ConsoleColor.Green) +
                login.Color(ConsoleColor.Yellow) +
                "-".Color(ConsoleColor.DarkGray) +
                $"{seen.SessionEndUtc.AddHours(session.TimeZone):HH:mm} ".Color(ConsoleColor.White) +
                seen.QuitMessage.Color(ConsoleColor.Blue);
        }
    }
}
