using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Commands
{
    public static class FindMessages
    {
        public static void FindByKeyword(BbsSession session, string searchTerm)
        {
            searchTerm = (searchTerm ?? string.Empty).ToLower();
            FindBy(session, searchTerm, c => c.Message.ToLower().Contains(searchTerm), "containing");
        }

        public static void FindBySender(BbsSession session, string sender)
        {
            var user = session.UserRepo.Get(u => u.Name, sender)?.FirstOrDefault();
            if (user == null)
                return;

            var results = DI.GetRepository<Chat>().Get(new Dictionary<string, object>
                {
                    {nameof(Chat.FromUserId), user.Id},
                    {nameof(Chat.ChannelId), session.Channel.Id}
                })
                ?.OrderByDescending(c => c.Id)
                ?.Take(Constants.MaxSearchResults);

            if (true == results?.Any())
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.DarkCyan))
                {
                    session.Io.OutputLine($"Messages numbers from {user.Name}, most recent first, limited to at most {Constants.MaxSearchResults} matches.");
                    session.Io.OutputLine();
                    string allResults = string.Join(", ", results.Select(c => $"{session.Chats.ItemNumber(c.Id)}"));
                    session.Io.OutputLine(allResults);
                }
            }
        }

        public static void FindByStartsWith(BbsSession session, string searchTerm)
        {
            searchTerm = (searchTerm ?? string.Empty).ToLower();
            FindBy(session, searchTerm, c => c.Message.ToLower().StartsWith(searchTerm), "starting with");
        }

        private static void FindBy(BbsSession session, string searchTerm, Func<Chat, bool> predicate, string matchDescription)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 3)
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    session.Io.OutputLine("Search term too small");
                    return;
                }
            }

            searchTerm = searchTerm.ToLower();

            var results = DI.GetRepository<Chat>().Get(c => c.ChannelId, session.Channel.Id)
                ?.Where(c => predicate(c))
                ?.OrderByDescending(c => c.Id)
                ?.Take(Constants.MaxSearchResults);

            if (true == results?.Any())
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.DarkCyan))
                {
                    session.Io.OutputLine($"Messages in this channel {matchDescription} the term '{searchTerm}', most recent first, limited to at most {Constants.MaxSearchResults} matches.");
                    session.Io.OutputLine();
                    string allResults = string.Join(Environment.NewLine, results.Select(c => $"{session.Chats.ItemNumber(c.Id)} : {GetExcerpt(c, searchTerm)}"));
                    session.Io.OutputLine(allResults);
                }
            }
        }

        private static string GetExcerpt(Chat chat, string searchTerm)
        {
            int p = chat.Message.IndexOf(searchTerm, StringComparison.CurrentCultureIgnoreCase);
            int start = Math.Max(0, p - 10);
            int end = Math.Min(chat.Message.Length - 1, p + 10);
            int len = end - start + 1;
            string excerpt = chat.Message.Substring(start, len);
            return excerpt;
        }

    }
}
