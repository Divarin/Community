using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Extensions;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class UserInfo
    {
        private static readonly MetadataType[] _filteredMetadataTypes = new[]
        {
            MetadataType.ReadBulletins,
            MetadataType.ReadMessages,
        };

        public static void Execute(BbsSession session, string username)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                User user = string.IsNullOrWhiteSpace(username) ? session.User : session.UserRepo.Get(u => u.Name, username)?.FirstOrDefault();
                if (user == null)
                {
                    session.Io.OutputLine($"User '{username}' not found.");
                    return;
                }

                var chanFlags = session.UcFlagRepo.Get(f => f.UserId, user.Id)
                    ?.Where(f => f.Flags.HasFlag(UCFlag.Moderator));

                var channels = DI.GetRepository<Channel>().Get()
                    .ToDictionary(k => k.Id);

                string moderatorOf = string.Empty;
                if (true == chanFlags?.Any())
                {
                    moderatorOf = string.Join(", ", chanFlags
                        .Select(f => channels.ContainsKey(f.ChannelId) ? channels[f.ChannelId].Name : "Deleted Channel")                        
                        .Distinct());
                }

                StringBuilder builder = new StringBuilder();
                builder.AppendLine("*** User Info ***");
                builder.AppendLine($"User ID      : {user.Id}");
                builder.AppendLine($"Username     : {user.Name}");
                builder.AppendLine($"First Login  : {user.DateAddedUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm:ss}");
                builder.AppendLine($"Last Login   : {user.LastLogonUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm:ss}");
                builder.AppendLine($"Total Logins : {user.TotalLogons}");
                builder.AppendLine($"Access       : {user.Access}");
                builder.AppendLine($"Moderator of : {moderatorOf}");
                builder.AppendLine($"Time Zone    : {user.Timezone}");
                builder.AppendLine($"Terminal     : {user.Cols} x {user.Rows}  {user.Emulation}");

                var metaRepo = DI.GetRepository<Metadata>();

                var metas = metaRepo
                    .Get(m => m.UserId, user.Id)
                    .Where(m => !_filteredMetadataTypes.Contains(m.Type))
                    .ToList();

                var profileMeta = metas.FirstOrDefault(x => x.Type == MetadataType.Profile);
                if (profileMeta != null)
                {
                    metas.Remove(profileMeta);
                }

                var wasDoing = metas?.FirstOrDefault(m => m.Type == MetadataType.WasDoing)?.Data;

                if (!string.IsNullOrWhiteSpace(wasDoing))
                {
                    builder.AppendLine($"Was Doing    : {wasDoing}");
                }

                var activeSessions = DI.Get<ISessionsList>()
                    .Sessions
                    .Where(s => s.User != null && s.User.Id == user.Id)
                    .ToList();
                builder.AppendLine("--- Active Sessions ---");
                if (activeSessions.Count < 1)
                {
                    builder.AppendLine("No active sessions (not online)");
                }
                else
                {
                    foreach (var s in activeSessions)
                    {
                        string listItem = $"{s.User.Name} ";
                        if (s.Afk)
                        {
                            if (!"away from keyboard".Equals(s.AfkReason, StringComparison.CurrentCultureIgnoreCase))
                                listItem += $"(AFK:{s.AfkReason})";
                            else
                                listItem += "(AFK)";
                        }

                        if (s.Items.ContainsKey(SessionItem.Doing) && !string.IsNullOrWhiteSpace(s.Items[SessionItem.Doing] as string))
                        {
                            listItem += $"({s.Items[SessionItem.Doing]})";
                        }

                        if (s.DoNotDisturb)
                            listItem += "(DND)";

                        listItem += $" in {s.Channel?.Name}";
                        var idleTime = s.IdleTime.TotalMinutes;
                        if (idleTime >= 5)
                        {
                            int h = (int)Math.Floor(idleTime / 60);
                            int m = (int)Math.Round(idleTime % 60);
                            if (h > 0)
                                listItem += $" - {h}h {m}m idle";
                            else
                                listItem += $" - {m} min. idle";
                        }

                        builder.AppendLine(listItem);
                    }
                }

                if (session.User.Access.HasFlag(AccessFlag.Administrator) && 'Y' == session.Io.Ask("See metadata?"))
                {
                    foreach (var meta in metas)
                        builder.AppendLine($"Meta         : {meta.Type} = {meta.Data}");
                }

                session.Io.OutputLine(builder.ToString(), OutputHandlingFlag.PauseAtEnd);
                builder.Clear();

                // profile
                if (profileMeta == null)
                {
                    builder.AppendLine($"{user.Name} has no profile.");
                    if (session.User.Id == user.Id)
                    {
                        builder.AppendLine("use /profile to create & edit your profile.");
                    }
                }
                else
                {
                    builder.AppendLine($"{user.Name}'s profile:");
                    var profile = profileMeta.Data;
                    if (profile.Length > Constants.MaxProfileLength)
                    {
                        profile = profile.Substring(0, Constants.MaxProfileLength);
                    }
                    builder.AppendLine(profile);
                }

                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Yellow))
                {
                    session.Io.OutputLine(builder.ToString(), OutputHandlingFlag.DoNotTrimStart | OutputHandlingFlag.PauseAtEnd);
                }
            }
        }
    }
}
