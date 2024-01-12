using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class Calendar
    {
        public static Count Count(BbsSession session)
        {
            var calRepo = DI.GetRepository<CalendarItem>();
            PruneOldEntries(calRepo);
            var cals = calRepo.Get();
            
            return new Count
            {
                TotalCount = cals.Count(),
                SubsetCount = cals.Count(x => x.DateCreatedUtc > session.User.LastLogonUtc)
            };
        }

        public static void Execute(BbsSession session)
        {
            var originalForegroundColor = session.Io.GetForeground();
            var previousLocation = session.CurrentLocation;
            var originalDnd = session.DoNotDisturb;

            try
            {
                session.DoNotDisturb = true;
                session.CurrentLocation = Module.Calendar;
                MenuItem menuSelection = MenuItem.Quit;
                var channelNames = DI.GetRepository<Channel>()
                    .Get()
                    .ToDictionary(k => k.Id, v => v.Name);
                var calRepo = DI.GetRepository<CalendarItem>();
                do
                {
                    var menuResult = ShowMenu(session, calRepo, channelNames);
                    menuSelection = menuResult.Item1;
                    CalendarItem selectedItem = menuResult.Item2;
                    switch (menuSelection)
                    {
                        case MenuItem.AddItem:
                            AddNewItem(session, calRepo, channelNames);
                            break;
                        case MenuItem.DeleteItem:
                            if (selectedItem != null)
                                DeleteItem(session, calRepo, selectedItem);
                            break;
                        case MenuItem.RenewItem:
                            if (selectedItem != null)
                                RenewItem(session, calRepo, selectedItem);
                            break;
                    }
                } while (menuSelection != MenuItem.Quit);
            }
            finally
            {
                session.Io.SetForeground(originalForegroundColor);
                session.CurrentLocation = previousLocation;
                session.DoNotDisturb = originalDnd;
            }
        }

        private static Tuple<MenuItem, CalendarItem> ShowMenu(BbsSession session, IRepository<CalendarItem> calRepo, IDictionary<int, string> channelNames)
        {
            var items = calRepo.Get()
                .OrderByDescending(c => c.DateCreatedUtc)
                .ToArray();

            session.Io.SetForeground(ConsoleColor.Magenta);
            session.Io.OutputLine($"{Constants.Inverser}*** Live-Chat Calendar ***{Constants.Inverser}");
            session.Io.SetForeground(ConsoleColor.Cyan);
            session.Io.OutputLine($"Calendar entries are deleted after {Constants.MaxCalendarItemDays} days unless the owner renews them.");
            session.Io.SetForeground(ConsoleColor.Blue);
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                string ch = item.ChannelId.HasValue && channelNames.ContainsKey(item.ChannelId.Value) ? channelNames[item.ChannelId.Value] : "No particular channel";
                string un = session.Usernames.ContainsKey(item.UserId) ? session.Usernames[item.UserId] : "Unknown";

                builder.AppendLine($"{i + 1,-4}   {item.EventTime}   Created: {item.DateCreatedUtc:MM-dd} By: {un}");
                builder.AppendLine($"Channel: {ch,-Constants.MaxChannelNameLength}   Topic: {item.Topic ?? "No particular topic"}");
                builder.AppendLine($"{Constants.Spaceholder}---");
            }
            session.Io.OutputLine(builder.ToString());
            session.Io.SetForeground(ConsoleColor.White);
            builder.Clear();
            builder.AppendLine("V : View calendar again (re-list)");
            builder.AppendLine("A : Add a new calendar item");
            builder.AppendLine("D : Delete an item");
            builder.AppendLine("R : Renew an item");
            builder.Append("Q : Quit");
            session.Io.OutputLine(builder.ToString());

            session.Io.SetForeground(ConsoleColor.Yellow);
            session.Io.Output($"{Constants.Inverser}[Calendar] >{Constants.Inverser} ");
            var k = session.Io.InputKey();
            session.Io.OutputLine();

            Func<string, CalendarItem> GetItemByNumber = s =>
            {
                session.Io.Output($"{s} what item number?: ");
                var n = session.Io.InputLine();
                session.Io.OutputLine();
                if (!string.IsNullOrWhiteSpace(n) && int.TryParse(n, out int i) && i >= 1 && i <= items.Length)
                    return items[i-1];
                return null;
            };

            switch (k)
            {
                case 'a': case 'A': return new Tuple<MenuItem, CalendarItem>(MenuItem.AddItem, null);
                case 'v': case 'V': return new Tuple<MenuItem, CalendarItem>(MenuItem.ViewCalendar, null);
                case 'd':
                case 'D':
                    {
                        var item = GetItemByNumber("Delete");
                        if (item != null)
                            return new Tuple<MenuItem, CalendarItem>(MenuItem.DeleteItem, item);
                    }
                    break;
                case 'r':
                case 'R':
                    {
                        var item = GetItemByNumber("Renew");
                        if (item != null)
                            return new Tuple<MenuItem, CalendarItem>(MenuItem.RenewItem, item);
                    }
                    break;
                default: return new Tuple<MenuItem, CalendarItem>(MenuItem.Quit, null);
            }

            return new Tuple<MenuItem, CalendarItem>(MenuItem.ViewCalendar, null);
        }

        private static void AddNewItem(BbsSession session, IRepository<CalendarItem> calRepo, IDictionary<int, string> channelNames)
        {
            Func<string, int> GetChannelIdByName = cn =>
            {
                foreach (var chan in channelNames)
                    if (chan.Value.Equals(cn, StringComparison.CurrentCultureIgnoreCase))
                        return chan.Key;
                return -1;
            };

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Cyan))
            {
                CalendarItem item = new CalendarItem()
                {
                    DateCreatedUtc = DateTime.UtcNow,
                    UserId = session.User.Id
                };

                session.Io.OutputLine($"Describe when you'll be online to chat.  Examples:{session.Io.NewLine}" +
                    $"Every day at 4:30 pm pacific time{session.Io.NewLine}" +
                    $"Tuesdays at 7:00 am eastern{session.Io.NewLine}" +
                    $"June 15th at 19:00 UTC");
                session.Io.Output("When?: ");
                var when = session.Io.InputLine();
                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(when))
                {
                    Error(session, "ABORTED!");
                    return;
                }
                item.EventTime = when;

                session.Io.Output($"{Constants.Inverser}Enter channel name or number, or enter for no channel in particular:{Constants.Inverser} ");
                var chan = session.Io.InputLine();
                session.Io.OutputLine();
                if (!string.IsNullOrWhiteSpace(chan))
                {
                    int chanNum;
                    if (int.TryParse(chan, out chanNum) && channelNames.ContainsKey(chanNum))
                        item.ChannelId = chanNum;
                    else if ((chanNum = GetChannelIdByName(chan)) > 0)
                        item.ChannelId = chanNum;
                    else
                    {
                        Error(session, "ABORTED!");
                        return;
                    }
                }

                session.Io.Output($"{Constants.Inverser}Enter topic to be discussed, or enter for no topic in particular:{Constants.Inverser} ");
                var topic = session.Io.InputLine();
                session.Io.OutputLine();
                if (!string.IsNullOrWhiteSpace(topic))
                    item.Topic = topic;

                calRepo.Insert(item);
                session.Io.OutputLine("Calendar item added!");
            }
        }

        private static void DeleteItem(BbsSession session, IRepository<CalendarItem> calRepo, CalendarItem item)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Cyan))
            {
                if (item == null)
                    Error(session, "Item not found!");
                else if (item.UserId != session.User.Id && !session.User.Access.HasFlag(AccessFlag.Administrator))
                    Error(session, "Not your item!");
                else
                {
                    calRepo.Delete(item);
                    session.Io.OutputLine("Item deleted.");
                }
            }
        }

        private static void RenewItem(BbsSession session, IRepository<CalendarItem> calRepo, CalendarItem item)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Cyan))
            {
                if (item == null)
                    Error(session, "Item not found");
                else if (item.UserId != session.User.Id && !session.User.Access.HasFlag(AccessFlag.Administrator))
                    Error(session, "Not your item");
                else
                {
                    item.DateCreatedUtc = DateTime.UtcNow;
                    calRepo.Update(item);
                    using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
                    {
                        session.Io.OutputLine($"Item renewed, it will expire in {Constants.MaxCalendarItemDays} days from now.");
                    }
                }
            }
        }

        private static void PruneOldEntries(IRepository<CalendarItem> calRepo)
        {
            var now = DateTime.UtcNow;
            var toBeDeleted = calRepo.Get()
                .Where(c => (now - c.DateCreatedUtc).TotalDays > Constants.MaxCalendarItemDays);
            if (true == toBeDeleted?.Any())
                calRepo.DeleteRange(toBeDeleted);
        }

        private static void Error(BbsSession session, string err)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
            {
                session.Io.OutputLine(err);
            }
        }

        private enum MenuItem
        {
            Quit,
            ViewCalendar,
            AddItem,
            DeleteItem,
            RenewItem
        }
    }
}
