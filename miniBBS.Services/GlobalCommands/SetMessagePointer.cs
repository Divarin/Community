using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Services.GlobalCommands
{
    public static class SetMessagePointer
    {
        /// <summary>
        /// Tries to set the msg pointer to the <paramref name="msgPointer"/> or the nearest next message in the active channel. 
        /// Returns true if the message pointer has changed or false if it did not (at end of messages). 
        /// Also saves the user's channel's msg pointer in the database for their next login
        /// </summary>
        public static bool Execute(BbsSession session, int msgPointer, bool reverse = false)
        {
            int oldMsgPointer = session.MsgPointer;

            if (true != session.Chats?.Any())
                msgPointer = 0;
            else
            {
                msgPointer = reverse ?
                    session.Chats.Keys.LastOrDefault(k => k <= msgPointer)  : 
                    session.Chats.Keys.FirstOrDefault(k => k >= msgPointer);

                if (msgPointer == default)
                    msgPointer = reverse ?
                        session.Chats.Keys.First() : 
                        session.Chats.Keys.Max();
            }

            bool changed = msgPointer != oldMsgPointer;

            if (changed)
            {
                session.UcFlag.LastReadMessageNumber = msgPointer;
                session.UcFlag = session.UcFlagRepo.InsertOrUpdate(session.UcFlag);
                if (session.MsgPointer > 0)
                    session.LastMsgPointer = session.MsgPointer;
                else
                    session.LastMsgPointer = msgPointer;
                session.MsgPointer = msgPointer;
            }
            else if (reverse)
            {
                if (!session.Items.ContainsKey(SessionItem.ShowChatArchive) ||
                    (bool)session.Items[SessionItem.ShowChatArchive] != true)
                {
                    session.Io.Error("There may be earlier messages, use '" + "/archive".Color(ConsoleColor.Yellow) + "' to unlock the archive!");
                }
            }

            session.ContextPointer = null;
            return changed;
        }

        public static void SetToFirstUnreadMessage(BbsSession session)
        {
            var _readIds = session.ReadChatIds(GlobalDependencyResolver.Default);
            bool _found = false;
            var _firstUnread = session.Chats.Keys.FirstOrDefault(x =>
            {
                if (!_readIds.Contains(x))
                {
                    _found = true;
                    return true;
                }
                return false;
            });
            if (_found)
            {
                Execute(session, _firstUnread);
                ShowNextMessage.Execute(session, ChatWriteFlags.UpdateLastReadMessage | ChatWriteFlags.UpdateLastMessagePointer);
            }
            else
                session.Io.Error("You have no unread messages in this channel.");
        }

        public static void SetToDate(BbsSession session, string date)
        {
            DateTime? parsedDate = null;
            if (string.IsNullOrWhiteSpace(date))
                parsedDate = session.User.LastLogonUtc;
            else if (DateTime.TryParse(date, out DateTime pd))
                parsedDate = pd;
            else
            {
                var days = new[] { "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT" };
                var longdays = new[] { "SUNDAY", "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY" };
                var ix = days.IndexOf(x => x.Equals(date, StringComparison.OrdinalIgnoreCase));
                if (ix < 0)
                    ix = longdays.IndexOf(x => x.Equals(date, StringComparison.OrdinalIgnoreCase));
                if (ix >= 0)
                    parsedDate = GetDateFromDay((DayOfWeek)ix);
                else
                {
                    session.Io.Error($"Unable to parse '{date}' as a date.");
                    return;
                }
            }

            var target = session.Chats.FirstOrDefault(x => x.Value.DateUtc >= parsedDate);
            if (target.Value == null)
            {
                session.Io.Error($"Unable to find first chat with target date: {parsedDate:yyyy/MM/dd}, you may need to show archived messages with '/archive' first.");
                return;
            }

            session.Io.OutputLine($"Set pointer to first message posted on or after {parsedDate:yyyy/MM/dd}.".Color(ConsoleColor.Magenta));

            Execute(session, target.Key);
            ShowNextMessage.Execute(session, ChatWriteFlags.UpdateLastReadMessage | ChatWriteFlags.UpdateLastMessagePointer);
        }

        private static DateTime GetDateFromDay(DayOfWeek dayIndex)
        {
            var date = DateTime.UtcNow.Date.AddDays(-1);
            while (date.DayOfWeek != dayIndex)
                date = date.AddDays(-1);
            return date;
        }
    }
}
