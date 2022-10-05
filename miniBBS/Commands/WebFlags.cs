using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using miniBBS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Commands
{
    public static class WebFlags
    {
        public static void SetChannelWebVisibility(BbsSession session, bool visible)
        {
            // see if user can do this
            bool isModerator =
                session.User.Access.HasFlag(AccessFlag.Administrator) ||
                session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                session.UcFlag.Flags.HasFlag(UCFlag.Moderator);

            if (session.Channel.AutoWebVisible == visible || !isModerator)
                return;

            var channelRepo = DI.GetRepository<Channel>();
            session.Channel.AutoWebVisible = visible;
            channelRepo.Update(session.Channel);
            string message = $"{session.User.Name} has made chats in channel {session.Channel.Name} {(visible ? "" : "not ")}automatically visible on the web.";

            DI.Get<ILogger>().Log(session, message);
            session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, message));
            session.Io.OutputLine(message);

            // if setting to false ask if user wants to make all old chats no longer web visible
            if (!visible && 'Y' == session.Io.Ask("Do you want to retroactivally make all chats in this channel non-web visible?"))
            {
                var chatRepo = DI.GetRepository<Chat>();
                var chatsToUpdate = chatRepo.Get(new Dictionary<string, object>
                {
                    {nameof(Chat.ChannelId), session.Channel.Id},
                    {nameof(Chat.WebVisible), true}
                }).ToList();
                if (true == chatsToUpdate?.Any())
                {
                    for (int i=0; i < chatsToUpdate.Count; i++)
                    {
                        var chat = chatsToUpdate[i];
                        chat.WebVisible = false;
                        chatRepo.Update(chat);
                        if (session.Chats.ContainsKey(chat.Id))
                            session.Chats[chat.Id].WebVisible = visible;
                    }
                }
            }
        }

        public static void SetChatWebVisibility(BbsSession session, bool visible, int chatNum)
        {
            // see if user can do this
            // if not visible then also allow global moderator or channel moderator

            var chatId = session.Chats.ItemKey(chatNum);
            if (!chatId.HasValue)
            {
                session.Io.Error("Invalid message number.");
                return;
            }

            var chat = session.Chats[chatId.Value];

            bool canUpdate =
                chat.FromUserId == session.User.Id ||
                session.User.Access.HasFlag(AccessFlag.Administrator) ||
                ( !visible && 
                  ( 
                    session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                    session.UcFlag.Flags.HasFlag(UCFlag.Moderator)
                  ) );

            if (!canUpdate)
            {
                session.Io.Error("Access denied for you to update this chat.");
                return;
            }

            chat.WebVisible = visible;
            DI.GetRepository<Chat>().Update(chat);
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                session.Io.OutputLine($"Message {chatNum} is {(visible ? "now" : "not")} web-visible.");
                session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, $"{session.User.Name} has flagged message {chatNum} to be {(visible ? "now" : "not")} web-visible."));
                DI.Get<IWebLogger>().SetForceCompile();
            }
        }

        public static void SetUserChatWebVisibility(BbsSession session, bool visible)
        {
            var metaRepo = DI.GetRepository<Metadata>();
            var metas = metaRepo.Get(new Dictionary<string, object>
            {
                {nameof(Metadata.Type), MetadataType.UserChatWebVisibility},
                {nameof(Metadata.UserId), session.User.Id}
            });

            Metadata meta;

            if (metas.Count() > 1)
            {
                var maxId = metas.Max(x => x.Id);
                var toBeDeleted = metas.Where(x => x.Id != maxId).ToList();
                metaRepo.DeleteRange(toBeDeleted);
                meta = metas.FirstOrDefault(x => x.Id == maxId);
            }
            else if (metas.Any())
                meta = metas.First();
            else
                meta = new Metadata
                {
                    Type = MetadataType.UserChatWebVisibility,
                    UserId = session.User.Id
                };

            meta.Data = visible.ToString();
            metaRepo.InsertOrUpdate(meta);
            session.Items[SessionItem.UserChatWebVisibility] = visible;

            var msg = visible ?
                "From now on all posts you write will be flagged as visible on the web." :
                "From now on all posts you write will be flagged as not visible on the web.";

            session.Io.Error(msg);

            msg = visible ?
                $"{session.User.Name} will have their future posts visible on the web." :
                $"{session.User.Name} will have their future posts not visible on the web.";

            if (session.Channel != null)
                session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, msg));
        }

        public static void UserPreferenceDialog(BbsSession session)
        {
            var di = GlobalDependencyResolver.Default;            

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
            {
                bool exitMenu = false;
                do
                {
                    var preference = session.UserWebChatPreference(di);
                    session.Io.OutputLine("*** " + "Website Visibility Preferences".Color(ConsoleColor.Red) + " ***");
                    session.Io.OutputLine("Note: Your preference trumps the channel's preference.");
                    if (session.Channel.AutoWebVisible)
                        session.Io.OutputLine($"Messages in Channel " + session.Channel.Name.Color(ConsoleColor.Yellow) + " WILL".Color(ConsoleColor.Red) + " be web-visible if you have no preference.");
                    else
                        session.Io.OutputLine($"Messages in Channel " + session.Channel.Name.Color(ConsoleColor.Yellow) + " will " + "NOT".Color(ConsoleColor.Red) + " be web-visible if you have no preference.");
                    if (!preference.HasValue)
                        session.Io.OutputLine("You currently have no preference so it will default to the channel's preference.");
                    else
                        session.Io.OutputLine($"Based on your preference your messages {(preference == true ? "WILL" : "will NOT").Color(ConsoleColor.Red)} be web-visible.");
                    session.Io.OutputLine("Options:");
                    session.Io.OutputLine("1)".Color(ConsoleColor.Green) + " Set your preference to be web-visible.");
                    session.Io.OutputLine("2)".Color(ConsoleColor.Green) + " Set your preference to not be web-visible.");
                    session.Io.OutputLine("3)".Color(ConsoleColor.Green) + " Have no preference one way or the other.");
                    session.Io.OutputLine("4)".Color(ConsoleColor.Green) + " What is all this web stuff?");
                    session.Io.OutputLine("Q)".Color(ConsoleColor.Green) + " Quit.");
                    var k = session.Io.Ask("[Web Prefs]");
                    switch (k)
                    {
                        case '1': SetUserChatWebVisibility(session, true); break;
                        case '2': SetUserChatWebVisibility(session, false); break;
                        case '3': RemoveUserChatWebPreference(session); break;
                        case '4': Menus.Web.Show(session); break;
                        case 'Q': exitMenu = true; break;
                    }
                } while (!exitMenu);
            }
        }

        private static void RemoveUserChatWebPreference(BbsSession session)
        {
            var metaRepo = DI.GetRepository<Metadata>();
            var metas = metaRepo.Get(new Dictionary<string, object>
            {
                {nameof(Metadata.Type), MetadataType.UserChatWebVisibility},
                {nameof(Metadata.UserId), session.User.Id}
            });
            
            if (true == metas?.Any())
                metaRepo.DeleteRange(metas);

            var userSessions = DI.Get<ISessionsList>()
                .Sessions
                .Where(s => s.User?.Id == session.User.Id)
                .ToList();

            foreach (var s in userSessions)
            {
                if (s.Items.ContainsKey(SessionItem.UserChatWebVisibility))
                    s.Items.Remove(SessionItem.UserChatWebVisibility);
            }
        }
    }
}
