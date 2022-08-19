using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class Pin
    {
        public static void Execute(BbsSession session, params string[] args)
        {
            int? msgNum = null;
            int? msgId = null;

            if (args?.Length >= 1 && int.TryParse(args[0], out int n))
            {
                msgNum = n;
                if (msgNum.HasValue)
                    msgId = session.Chats.ItemKey(msgNum.Value);
            }
            else
                msgId = session.LastReadMessageNumber;

            if (!msgId.HasValue)
            {
                session.Io.Error("Invalid message number");
                return;
            }

            var msg = session.Chats[msgId.Value];

            if (msg.Id != session.LastReadMessageNumber)
            {
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
                {
                    msg.Write(session, false, true);
                    var r = session.Io.Ask("Pin this message?");
                    if (r == 'N')
                        return;
                }
            }

            var pinRepo = DI.GetRepository<PinnedMessage>();
            var userPins = pinRepo.Get(p => p.PinnedByUserId, session.User.Id);
            var existing = userPins.Where(p => p.ChannelId == msg.ChannelId && p.MessageId == msg.Id).ToList();

            if (!session.User.Access.HasFlag(AccessFlag.Administrator) && userPins.Count() - existing.Count() > Constants.MaxPinsPerUser)
            {
                session.Io.Error("Sorry you have too many pins already, try deleting some with '/unpin'.");
                return;
            }

            if (true == existing?.Any())
                pinRepo.DeleteRange(existing);

            var pin = new PinnedMessage
            {
                ChannelId = msg.ChannelId,
                DatePinnedUtc = DateTime.UtcNow,
                MessageId = msg.Id,
                PinnedByUserId = session.User.Id,
                Private = true == args?.Any(a => a.StartsWith("p", StringComparison.CurrentCultureIgnoreCase))
            };
            pinRepo.Insert(pin);

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                session.Io.OutputLine("Message pinned");
        }

        public static void Unpin(BbsSession session, params string[] args)
        {
            if (true != args?.Any() || !int.TryParse(args[0], out int msgNum))
            {
                session.Io.Error("Usage: '/unpin 123' where '123' is the message number to unpin.");
                return;
            }

            int? msgId = session.Chats.ItemKey(msgNum);
            if (!msgId.HasValue) session.Io.Error("Invalid message number");
            var msg = session.Chats[msgId.Value];

            var pinRepo = DI.GetRepository<PinnedMessage>();
            var pins = pinRepo.Get(new Dictionary<string, object>
            {
                {nameof(PinnedMessage.ChannelId), session.Channel.Id},
                {nameof(PinnedMessage.PinnedByUserId), session.User.Id},
                {nameof(PinnedMessage.MessageId), msg.Id}
            });

            if (true != pins?.Any())
            {
                session.Io.Error("You don't have that message pinned.");
                return;
            }

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                msg.Write(session, false, true);
                var r = session.Io.Ask("Unpin this message?");
                if (r == 'N')
                    return;
            }

            pinRepo.DeleteRange(pins);

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                session.Io.OutputLine("Message pin removed");
        }

        public static void ShowPins(BbsSession session, params string[] args)
        {
            var pinRepo = DI.GetRepository<PinnedMessage>();
            
            var filters = new Dictionary<string, object>();
            filters[nameof(PinnedMessage.ChannelId)] = session.Channel.Id;
            
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                var option = session.Io.Ask($"Channel Pins to Show{Environment.NewLine}(M)y pins, (A)ll public pins, (Q)uit");
                switch (option)
                {
                    case 'M':
                        filters[nameof(PinnedMessage.PinnedByUserId)] = session.User.Id;
                        break;
                    case 'A':
                        filters[nameof(PinnedMessage.Private)] = false;
                        break;
                    default:
                        return;
                }
            
                var pins = pinRepo.Get(filters).OrderByDescending(p => p.DatePinnedUtc);
                if (true != pins?.Any())
                {
                    session.Io.Error("No pins");
                    return;
                }

                var builder = new StringBuilder();
                var odd = false;
                foreach (var pin in pins)
                {
                    var msgNum = session.Chats.ItemNumber(pin.MessageId);
                    if (!msgNum.HasValue)
                        continue;
                    var msg = session.Chats[pin.MessageId];
                    var username = session.Usernames.ContainsKey(msg.FromUserId) ? session.Usernames[msg.FromUserId] : "Unknown";

                    ConsoleColor clr = odd ? ConsoleColor.Blue : ConsoleColor.Cyan;
                    var reNum = msg.ResponseToId.HasValue ? session.Chats.ItemNumber(msg.ResponseToId.Value) : null;

                    builder.AppendLine($"{Constants.InlineColorizer}{(int)clr}{Constants.InlineColorizer}{(pin.Private ? "(pvt) " : "")}{msgNum} : [{msg.DateUtc:yy-MM-dd HH:mm}] <{username}> re:{reNum}");
                    builder.AppendLine(msg.Message.MaxLength(session.Cols-6));
                    odd = !odd;
                }
                builder.AppendLine($"{Constants.InlineColorizer}-1{Constants.InlineColorizer}");

                session.Io.OutputLine(builder.ToString(), OutputHandlingFlag.PauseAtEnd);
            }
        }
    }
}
