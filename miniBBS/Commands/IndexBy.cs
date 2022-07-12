using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class IndexBy
    {
        public static void Execute(BbsSession session, string indexType)
        {
            if (string.IsNullOrWhiteSpace(indexType))
            {
                ShowUsage(session);
                return;
            }

            switch (indexType.ToLower())
            {
                case "date":
                    Date(session);
                    return;
                case "new":
                    New(session);
                    return;
                default:
                    ShowUsage(session);
                    return;
            }
        }

        private static void Date(BbsSession session)
        {
            if (true != session.Chats?.Any())
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    session.Io.OutputLine("No chats in this channel.");
                }
                return;
            }

            DateTime? lastDay = null;

            Stack<string> lines = new Stack<string>();

            foreach (var chat in session.Chats.Values)
            {
                DateTime thisDate = chat.DateUtc.AddDays(session.TimeZone).Date;
                if (!lastDay.HasValue || lastDay.Value != thisDate)
                {
                    lastDay = thisDate;
                    string username = session.Usernames.ContainsKey(chat.FromUserId) ? session.Usernames[chat.FromUserId] : "Unknown";
                    lines.Push($"{session.Chats.ItemNumber(chat.Id)} : {chat.DateUtc.AddHours(session.TimeZone):MM-dd HH:mm} : <{username}> {chat.Message.MaxLength(Constants.MaxSnippetLength)}");
                }
            }

            lines.Push("Index of the first message of each day, most recent first:");

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                StringBuilder builder = new StringBuilder();
                while (lines.Count > 0)
                    builder.AppendLine(lines.Pop());
                session.Io.OutputLine(builder.ToString());
            }
        }

        private static void New(BbsSession session)
        {
            if (true != session.Chats?.Any())
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    session.Io.OutputLine("No chats in this channel.");
                }
                return;
            }

            Stack<string> lines = new Stack<string>();

            foreach (var chat in session.Chats.Values)
            {
                if (!chat.ResponseToId.HasValue)
                {
                    string username = session.Usernames.ContainsKey(chat.FromUserId) ? session.Usernames[chat.FromUserId] : "Unknown";
                    lines.Push($"{session.Chats.ItemNumber(chat.Id)} : {chat.DateUtc.AddHours(session.TimeZone):MM-dd HH:mm} : <{username}> {chat.Message.MaxLength(Constants.MaxSnippetLength)}");
                }
            }

            lines.Push("Index of new messages, most recent first:");

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                StringBuilder builder = new StringBuilder();
                while (lines.Count > 0)
                    builder.AppendLine(lines.Pop());
                session.Io.OutputLine(builder.ToString());
            }
        }

        private static void ShowUsage(BbsSession session)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
            {
                session.Io.OutputLine("Usage: /index (index type)");
                session.Io.OutputLine("Index Types:");
                session.Io.OutputLine("/index date  :  shows the first post from each day.");
                session.Io.OutputLine("/index new   :  shows new messages (msgs without 're:' numbers).");
            }
        }

    }
}
