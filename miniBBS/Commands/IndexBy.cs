using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions_Collection;
using miniBBS.Extensions_ReadTracker;
using miniBBS.Extensions_String;
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
                case "length":
                    Length(session);
                    return;
                case "links":
                case "link":
                    TextFilesLinks(session);
                    return;
                case "unread":
                    Unread(session);
                    return;
                default:
                    ShowUsage(session);
                    return;
            }
        }

        private static void Unread(BbsSession session)
        {
            if (true != session.Chats?.Any())
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    session.Io.OutputLine("No chats in this channel.");
                }
                return;
            }

            var readIds = session.ReadChatIds(DI.Get<IDependencyResolver>());
            var builder = new StringBuilder(); 
            builder.AppendLine("Index of unread messages, oldest first:");

            foreach (var line in session.Chats
                        .Where(c => !readIds.Contains(c.Key))
                        .Select(c => FormatLine(session, c.Value)))
            {
                builder.AppendLine(line);
            }

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine(builder.ToString());
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
                    lines.Push(FormatLine(session, chat));
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
                    lines.Push(FormatLine(session, chat));
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

        private static void Length(BbsSession session)
        {
            if (true != session.Chats?.Any())
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    session.Io.OutputLine("No chats in this channel.");
                }
                return;
            }

            var chats = session.Chats.Values
                .OrderByDescending(c => c.Message.Length)
                .Take(100)
                .Select(chat => FormatLine(session, chat));

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("Index of longest messages, longest first:");
                foreach (var c in chats)
                    builder.AppendLine(c);
                session.Io.OutputLine(builder.ToString());
            }
        }

        private static void TextFilesLinks(BbsSession session)
        {
            if (true != session.Chats?.Any())
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    session.Io.OutputLine("No chats in this channel.");
                }
                return;
            }

            var chats = session.Chats.Values
                .Where(c => c.Message.StartsWith("TextFile Link:"))
                .OrderByDescending(c => c.DateUtc)
                .Take(100)
                .Select(chat => FormatLine(session, chat));

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("Index of messages with links to a text file, most recent first:");
                foreach (var c in chats)
                    builder.AppendLine(c);
                session.Io.OutputLine(builder.ToString());
            }
        }

        private static void ShowUsage(BbsSession session)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
            {
                session.Io.OutputLine("Usage: /index (index type)");
                session.Io.OutputLine("Index Types:");
                session.Io.OutputLine("/index date   : shows the first post from each day.");
                session.Io.OutputLine("/index new    : shows starts of new threads (msgs without 're:' numbers).");
                session.Io.OutputLine("/index length : shows long messages.");
                session.Io.OutputLine("/index links  : shows messages with textfile links.");
                session.Io.OutputLine("/index unread : shows unread messages in current channel.");
            }
        }

        public static string FormatLine(BbsSession session, Chat chat)
        {
            string username = session.Usernames.ContainsKey(chat.FromUserId) ? session.Usernames[chat.FromUserId] : "Unknown";
            string formatted;
            formatted = $"{session.Chats.ItemNumber(chat.Id)} : {chat.DateUtc.AddHours(session.TimeZone):MM-dd HH:mm} : <{username}>";
            if (session.Cols < 80)
            {
                formatted += Environment.NewLine;
                formatted += $"{chat.Message.MaxLength(session.Cols - 8)}";
            }
            else
            {
                formatted += $" {chat.Message}";
                formatted = formatted.MaxLength(session.Cols - 8);
            }

            return formatted;
        }
    }
}
