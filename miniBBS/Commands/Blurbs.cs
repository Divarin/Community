using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Commands
{
    public static class Blurbs
    {
        private static IList<Blurb> _blurbs = null;
        private static readonly Random _random = new Random((int)(DateTime.Now.Ticks % int.MaxValue));

        public static void Execute(BbsSession session, string blurbText = null)
        {
            if (_blurbs == null) LoadBlurbs();

            if (string.IsNullOrWhiteSpace(blurbText))
                ShowRandom(session);
            else
                AddBlub(session, blurbText);
        }

        private static void ShowRandom(BbsSession session)
        {
            if (_blurbs == null) LoadBlurbs();
            if (true != _blurbs?.Any()) return;

            var n = _random.Next(0, _blurbs.Count);
            var blurb = _blurbs[n];
            ShowBlurb(session, blurb);
        }

        public static void BlurbAdmin(BbsSession session, params string[] args)
        {
            const string usage = "Usage: /blurbadmin list, /blurbadmin del #";

            if (args == null || args.Length < 1)
            {
                session.Io.Error(usage);
                return;
            }

            switch (args[0].ToLower())
            {
                case "list":
                    ListAllBlurbs(session);
                    break;
                case "del":
                    if (args.Length >= 2 && int.TryParse(args[1], out int n))
                        DeleteBlurb(session, n);
                    else
                        session.Io.Error("Invalid blurb number");
                    break;
                default:
                    session.Io.Error(usage);
                    break;
            }
        }

        private static void ShowBlurb(BbsSession session, Blurb blurb)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                string username = session.Usernames.ContainsKey(blurb.UserId) ? session.Usernames[blurb.UserId] : "Unknown";
                session.Io.OutputLine($"Blurb by {UserIoExtensions.WrapInColor(username, ConsoleColor.Yellow)} ({blurb.DateAddedUtc:yy-MM-dd HH:mm}): ");
                session.Io.OutputLine(blurb.BlurbText);
            }
        }

        private static void ListAllBlurbs(BbsSession session)
        {
            var blurbs = _blurbs.OrderByDescending(x => x.DateAddedUtc);
            string list = string.Join(session.Io.NewLine, blurbs.Select(b =>
            {
                var username = session.Usernames.ContainsKey(b.UserId) ? session.Usernames[b.UserId] : "Unknown";
                return $"{b.Id} : {username} : {b.BlurbText}";
            }));

            session.Io.OutputLine(list);
        }

        private static void DeleteBlurb(BbsSession session, int blurbId)
        {
            var blurbToDelete = _blurbs?.FirstOrDefault(b => b.Id == blurbId);
            if (blurbToDelete == null)
            {
                session.Io.Error($"Can't find blurb by ID {blurbId}");
                return;
            }
            ShowBlurb(session, blurbToDelete);

            var canDelete = 
                blurbToDelete.UserId == session.User.Id ||
                session.User.Access.HasFlag(AccessFlag.Administrator) ||
                session.User.Access.HasFlag(AccessFlag.GlobalModerator);

            if (!canDelete)
            {
                session.Io.Error("Access denied");
                return;
            }

            if ('Y' == session.Io.Ask("Delete this blurb?"))
            {
                _blurbs.Remove(blurbToDelete);
                DI.GetRepository<Blurb>().Delete(blurbToDelete);
                session.Io.OutputLine("Blurb deleted");
            }
        }

        private static void AddBlub(BbsSession session, string blurb)
        {
            if (string.IsNullOrWhiteSpace(blurb) || blurb.Length > Constants.MaxBlurbLength)
            {
                session.Io.Error($"Blurb cannot be blank or exceed {Constants.MaxBlurbLength} characters!");
                return;
            }

            var now = DateTime.UtcNow;
            if (_blurbs.Any(b => b.UserId == session.User.Id && b.DateAddedUtc.Date == now.Date))
            {
                session.Io.Error("You've already posted a blurb today!");
                return;
            }

            var repo = DI.GetRepository<Blurb>();

            if (_blurbs.Count >= Constants.MaxBlurbs)
            {
                var oldestDate = _blurbs.Min(b => b.DateAddedUtc);
                var oldestBlurb = _blurbs.FirstOrDefault(b => b.DateAddedUtc == oldestDate);
                repo.Delete(oldestBlurb);
                _blurbs.Remove(oldestBlurb);
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    string username = session.Usernames.ContainsKey(oldestBlurb.UserId) ? session.Usernames[oldestBlurb.UserId] : "Unknown";
                    session.Io.OutputLine($"Removing oldest Blurb by {UserIoExtensions.WrapInColor(username, ConsoleColor.Yellow)} ({oldestBlurb.DateAddedUtc:yy-MM-dd HH:mm}): ");
                    session.Io.OutputLine(oldestBlurb.BlurbText);
                }
            }

            var newBlurb = new Blurb
            {
                UserId = session.User.Id,
                DateAddedUtc = now,
                BlurbText = blurb
            };
            newBlurb = repo.Insert(newBlurb);
            _blurbs.Add(newBlurb);

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine("New blurb added!");
            }

            var msg = $"{session.User.Name} added a new Blurb: {newBlurb.BlurbText}";
            session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, msg));
        }

        private static void LoadBlurbs()
        {
            _blurbs = DI.GetRepository<Blurb>().Get().ToList();
        }
    }
}
