using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Extensions
{
    public static class ModelExtensions
    {
        public static void Write(this Chat chat, BbsSession session, bool updateLastReadMessageNumber = true, bool monochrome = false)
        {
            if (!session.Usernames.ContainsKey(chat.FromUserId))
            {
                string un = session.UserRepo.Get(chat.FromUserId)?.Name;
                if (!string.IsNullOrWhiteSpace(un))
                    session.Usernames[chat.FromUserId] = un;
            }

            var line = chat.GetWriteString(session, monochrome);
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Green))
            {
                session.Io.OutputLine(line);
            }

            if (updateLastReadMessageNumber)
                session.LastReadMessageNumber = chat.Id;
        }

        public static string GetWriteString(this Chat chat, BbsSession session, bool monochrome = false)
        {
            Func<string> endClr = () => 
                monochrome ? 
                string.Empty :
                $"{Constants.InlineColorizer}-1{Constants.InlineColorizer}";

            Func<ConsoleColor, string> clr = _clr =>
                monochrome ?
                string.Empty :
                $"{Constants.InlineColorizer}{(int)_clr}{Constants.InlineColorizer}";

            string username = session.Usernames.ContainsKey(chat.FromUserId) ? session.Usernames[chat.FromUserId] : $"Unknown (ID:{chat.FromUserId})";
            var chatNum = session.Chats.ItemNumber(chat.Id);
            var reNum = session.Chats.ItemNumber(chat.ResponseToId);

            var w = chat.WebVisible ? " w" : "";
            var line = string.Join("", new[]
            {
                $"{clr(ConsoleColor.Cyan)}[{clr(ConsoleColor.White)}{chatNum}{clr(ConsoleColor.White)}:",
                $"{clr(ConsoleColor.Blue)}{chat.DateUtc.AddHours(session.TimeZone):yy-MM-dd HH:mm}",
                $"{clr(ConsoleColor.Cyan)}] <",
                $"{clr(ConsoleColor.Yellow)}{username}{clr(ConsoleColor.Cyan)}>",
                $"{clr(ConsoleColor.DarkGray)}{w} re:{reNum}{Environment.NewLine}",
                $"{clr(ConsoleColor.Green)}{chat.Message}{endClr()}"
            });

            return line;
        }

        public static bool CanJoin(this Channel channel, BbsSession session)
        {
            var userFlags = session.UcFlagRepo.Get(f => f.UserId, session.User.Id)
                .ToDictionary(k => k.ChannelId);

            bool result = !channel.RequiresInvite ||
                session.User.Access.HasFlag(AccessFlag.Administrator) ||
                session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                (userFlags.ContainsKey(channel.Id) && (userFlags[channel.Id].Flags & (UCFlag.Invited | UCFlag.Moderator)) > 0);

            return result;
        }

        public static IEnumerable<User> GetModerators(this Channel channel, BbsSession session, bool includeAdminsAndGlobalMods)
        {
            List<User> users = new List<User>();

            if (includeAdminsAndGlobalMods)
            {
                var f = AccessFlag.Administrator | AccessFlag.GlobalModerator;
                users.AddRange(session.UserRepo.Get().Where(u => (u.Access & f) > 0));
            }

            var moderators = session.UcFlagRepo
                .Get(f => f.ChannelId, channel.Id)
                ?.Where(f => f.Flags.HasFlag(UCFlag.Moderator))
                ?.Select(f => f.UserId)
                ?.Distinct()
                ?.Select(id => session.UserRepo.Get(id));

            if (true == moderators?.Any())
                users.AddRange(moderators);

            return users;
        }

        public static bool? UserWebChatPreference(this BbsSession session, IDependencyResolver di)
        {
            // if the flag is missing from the session.Items, see if it's in the database as metadata
            if (!session.Items.ContainsKey(SessionItem.UserChatWebVisibility))
            {
                var metaRepo = di.GetRepository<Metadata>();
                var metas = metaRepo.Get(new Dictionary<string, object>
                {
                    {nameof(Metadata.Type), MetadataType.UserChatWebVisibility},
                    {nameof(Metadata.UserId), session.User.Id}
                });

                // if it is, try to add it to the session.Items so we don't have to do a database query next time
                if (true == metas?.Any())
                {
                    // there may be more than one (although there shouldn't be)
                    bool? flagValue = null;
                    if (metas.All(m => true.ToString().Equals(m.Data, StringComparison.CurrentCultureIgnoreCase)))
                        flagValue = true;
                    else if (metas.All(m => false.ToString().Equals(m.Data, StringComparison.CurrentCultureIgnoreCase)))
                        flagValue = false;

                    // if so only set the session.Items value if all database stored values agree (all false or all true)
                    if (flagValue.HasValue)
                    {
                        var sessionsList = di.Get<ISessionsList>();
                        var userSessions = sessionsList.Sessions.Where(s => s.User?.Id == session.User.Id)?.ToList();
                        // do it for all sessions for this user (they may be connected more than once)
                        foreach (var s in userSessions)
                            s.Items[SessionItem.UserChatWebVisibility] = flagValue.Value;

                        // also, if there was more than one, clean up the database by deleting all but one
                        if (metas.Count() > 1)
                        {
                            var maxId = metas.Max(m => m.Id);
                            var toBeDeleted = metas.Where(m => m.Id != maxId).ToList();
                            metaRepo.DeleteRange(toBeDeleted);
                        }
                    }
                }
            }

            // if after all that the user's preference is stored in the session.Items, return it
            if (session.Items.ContainsKey(SessionItem.UserChatWebVisibility))
            {
                var userPref = session.Items[SessionItem.UserChatWebVisibility];
                if (userPref is bool boolean)
                    return boolean;
            }

            return null;
        }

        /// <summary>
        /// Gets whether or not a post by this user in this channel would normally be web visible if not for the /web or /noweb commands
        /// </summary>
        public static bool WebVisiblePosts(this BbsSession session, IDependencyResolver di)
        {
            var user = session.User;
            var channel = session.Channel;
            if (user == null || channel == null)
                return false;

            // if the user has a preference, use it
            var userPreference = session.UserWebChatPreference(di);
            if (userPreference.HasValue)
                return userPreference.Value;

            // otherwise the user doesn't have any preference so it defaults to whether or not the channel has a preference.
            return session.Channel.AutoWebVisible;
        }
    }
}
