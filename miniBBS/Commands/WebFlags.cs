using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
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

        public static void SetChatWebVisibility(BbsSession session, bool visible, Chat chat)
        {
            // see if user can do this
            // if not visible then also allow global moderator or channel moderator
           
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

            session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, msg));
        }
    }
}
