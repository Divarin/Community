using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions_UserIo;
using miniBBS.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class ChannelVoice
    {
        public static void Execute(BbsSession session, string[] args)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                if (args == null || args.Length < 1)
                {
                    Menus.Voice.Show(session);
                    return;
                }

                var isModerator =
                    session.User.Access.HasFlag(AccessFlag.Administrator) ||
                    session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                    session.UcFlag.Flags.HasFlag(UCFlag.Moderator);

                if (!isModerator)
                {
                    session.Io.Error("Access denied");
                    return;
                }

                switch (args[0].ToLower())
                {
                    case "+v":
                        if (args.Length == 1)
                            SetChannelRequiresVoice(session, true);
                        else if (session.Channel.RequiresVoice)
                            ToggleUserVoice(session, args[1], true);
                        else
                            session.Io.Error($"Unknown user or user not in channel: {args[1]}");
                        break;
                    case "-v":
                        if (args.Length == 1)
                            SetChannelRequiresVoice(session, false);
                        else if (session.Channel.RequiresVoice)
                            ToggleUserVoice(session, args[1], false);
                        else
                            session.Io.Error($"Unknown user or user not in channel: {args[1]}");
                        break;
                    case "vlist":
                        ListAllVoices(session);
                        break;
                    case "vall":
                        GiveAllVoices(session);
                        break;
                    case "vnone":
                        RemoveAllVoices(session);
                        break;
                    case "vq":
                        HandleQueueOperation(session, args.Skip(1).ToArray());
                        break;
                }
            }
        }

        private static void SetChannelRequiresVoice(BbsSession session, bool requiresVoice)
        {
            session.Channel.RequiresVoice = requiresVoice;
            DI.GetRepository<Channel>().Update(session.Channel);
            if (requiresVoice)
                OutputAndPublish(session, $"{session.User.Name} has set {session.Channel.Name} to require voice to talk.  Use /voice to request voice.");
            else
                OutputAndPublish(session, $"{session.User.Name} has lifted the voice requirement for {session.Channel.Name}.  Everyone is free to talk in this channel.");
        }

        private static void ListAllVoices(BbsSession session)
        {
            var userIdsWithVoice = session.UcFlagRepo
                .Get(f => f.ChannelId, session.Channel.Id)
                .Where(f => f.Flags.HasFlag(UCFlag.HasVoice))
                .Select(f => f.UserId)
                .Distinct()
                .ToArray();

            if (true != userIdsWithVoice?.Any())
            {
                session.Io.Error($"No users have voice in {session.Channel.Name}!");
                return;
            }

            List<string> usersWithVoice = new List<string>();
            foreach (var userId in userIdsWithVoice)
            {
                string username = session.Usernames.ContainsKey(userId) ? session.Usernames[userId] : "Unknown";
                usersWithVoice.Add(username);
            }
            usersWithVoice.Sort();

            session.Io.OutputLine($"Users with voice in {session.Channel.Name}: {string.Join(", ", usersWithVoice)}");
        }

        private static void GiveAllVoices(BbsSession session)
        {
            var users = DI.Get<ISessionsList>().Sessions
                .Where(s => session.Channel.Id == s.Channel?.Id && s.User != null)
                ?.OrderBy(s => s.User.Name)
                ?.Select(s => s.User)
                ?.ToList() ?? new List<User>();

            foreach (var user in users)
            {
                ToggleUserVoice(session, user, true);
            }
        }

        private static void RemoveAllVoices(BbsSession session)
        {
            var flags = session.UcFlagRepo.Get(x => x.ChannelId, session.Channel.Id)
                ?.Where(f => f.Flags.HasFlag(UCFlag.HasVoice));

            if (true != flags?.Any())
                return;

            foreach (var flag in flags)
            {
                flag.Flags &= ~UCFlag.HasVoice;
                session.UcFlagRepo.Update(flag);
            }

            var msg = $"{session.User.Name} has removed voice from everybody in this channel.";
            session.Io.OutputLine(msg);
            session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, msg));
        }

        private static void ToggleUserVoice(BbsSession session, string username, bool giveVoice)
        {
            // find user from the active sessions
            var user = DI.Get<ISessionsList>().Sessions.Where(s =>
                    s.Channel?.Id == session.Channel.Id &&
                    username.Equals(s.User?.Name, StringComparison.CurrentCultureIgnoreCase))
                ?.FirstOrDefault()
                ?.User;

            // if not found show error and return
            if (user == null)
            {
                session.Io.Error($"User {username} not found in {session.Channel.Name}.");
                return;
            }

            ToggleUserVoice(session, user, giveVoice);
        }

        private static void ToggleUserVoice(BbsSession session, User user, bool giveVoice)
        { 
            // find the flag(s) for this user/channel (should be only one but you never know)
            var ucFlags = session.UcFlagRepo.Get(new Dictionary<string, object>
            {
                {nameof(UserChannelFlag.UserId), user.Id},
                {nameof(UserChannelFlag.ChannelId), session.Channel.Id}
            });

            // if not found, set one and insert it
            if (true != ucFlags?.Any())
            {
                var newFlag = new UserChannelFlag
                {
                    UserId = user.Id,
                    ChannelId = session.Channel.Id,
                    Flags = UCFlag.None
                };
                newFlag = session.UcFlagRepo.Insert(newFlag);
                ucFlags = new[] { newFlag };
            }

            // set the voice flag
            foreach (var flag in ucFlags)
            {
                if (giveVoice)
                    flag.Flags |= UCFlag.HasVoice;
                else
                    flag.Flags &= ~UCFlag.HasVoice;
                session.UcFlagRepo.Update(flag);
            }

            var msg = $"{session.User.Name} has given channel voice to {user.Name}.";
            session.Io.OutputLine(msg);
            session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, msg));
        }

        private static void HandleQueueOperation(BbsSession session, params string[] args)
        {
            string msg = null;

            if (args == null || args.Length < 1)
            {
                TryShowQueue(session);
                return;
            }

            if (int.TryParse(args[0], out int duration) && duration >= 1)
            {
                var origVoice = session.Channel.RequiresVoice;
                var q = VoiceRequestQueueManager.CreateQueue(session.Channel.Id, TimeSpan.FromMinutes(duration));
                q.OnExpire = () =>
                {
                    var repo = DI.GetRepository<Channel>();
                    var chan = repo.Get(q.ChannelId);
                    msg = $"The voice request queue in {chan.Name} has expired.";
                    if (!origVoice)
                    {
                        chan.RequiresVoice = false;
                        repo.Update(chan);
                        if (chan.Id == session.Channel.Id)
                            session.Channel.RequiresVoice = false;
                        msg += $"  Voice is no longer required for this channel.";
                    }

                    session.Io.Output(msg);
                    session.Messager.Publish(session, new ChannelMessage(session.Id, chan.Id, msg));
                };
                msg = $"{session.User.Name} has created a voice request queue for the channel.  To request voice use '/voice'.";
                session.Io.OutputLine(msg);
                session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, msg));
                return;
            }

            switch (args[0].ToLower())
            {
                case "+":
                    {
                        var nextUserId = VoiceRequestQueueManager.GetQueue(session.Channel.Id)?.Dequeue();
                        if (nextUserId.HasValue)
                        {
                            var user = session.UserRepo.Get(nextUserId.Value);
                            if (user != null)
                                ToggleUserVoice(session, user, true);
                        }
                        else
                            session.Io.Error("No more users in the voice request queue.");
                    }
                    break;
                case "-":
                    {
                        var q = VoiceRequestQueueManager.GetQueue(session.Channel.Id);
                        var nextUserId = q?.Peek();
                        if (nextUserId.HasValue)
                        {
                            var user = session.UserRepo.Get(nextUserId.Value);
                            var username = session.Usernames.ContainsKey(user.Id) ? session.Usernames[user.Id] : "Unknown";
                            if ('Y' == session.Io.Ask($"Remove next user ({username}) from voice request queue?"))
                            {
                                q.Dequeue();
                                session.Io.OutputLine("Done.");
                            }
                        }
                    }
                    break;
                case "extend":
                    {
                        if (args.Length >= 2 && int.TryParse(args[1], out int additionalDuration) && additionalDuration >= 1)
                        {
                            var q = VoiceRequestQueueManager.GetQueue(session.Channel.Id);
                            if (q == null)
                                session.Io.Error("No voice request queue found for the current channel");
                            else
                            {
                                q.Extend(TimeSpan.FromMinutes(additionalDuration));
                                session.Io.OutputLine($"Added {additionalDuration} minutes to voice request queue.");
                            }
                        }
                        else
                            session.Io.Error("Missing or invalid arument for additional duration.  Usage: '/ch vq extend 5' where '5' is number of minutes.");
                    }
                    break;
                case "end":
                    {
                        var q = VoiceRequestQueueManager.GetQueue(session.Channel.Id);
                        if (q == null)
                            session.Io.Error("No voice request queue found for this channel.");
                        else
                        {
                            VoiceRequestQueueManager.RemoveQueue(session.Channel.Id);
                            msg = $"{session.User.Name} has removed the voice request queue from the channel.";
                            session.Io.OutputLine(msg);
                            session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, msg));
                            if ('Q' == session.Io.Ask("Remove voice requirement on channel?"))
                                SetChannelRequiresVoice(session, false);
                        }
                    }
                    break;
                default:
                    session.Io.Error($"Unrecognized 'vq' command: {args[0]}");
                    break;
            }

        }

        private static void TryShowQueue(BbsSession session)
        {
            // show queue (if any)
            var q = VoiceRequestQueueManager.GetQueue(session.Channel.Id);
            if (q == null)
                session.Io.Error("No voice request queue found for this channel");
            else
            {
                var builder = new StringBuilder();
                foreach (var userId in q.PeekAll())
                {
                    var username = session.Usernames.ContainsKey(userId) ? session.Usernames[userId] : "Unknown";
                    builder.AppendLine(username);
                }
                session.Io.Output(builder.ToString());
            }
        }

        private static void OutputAndPublish(BbsSession session, string message)
        {
            session.Io.OutputLine(message);
            session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, message));
        }

    }
}
