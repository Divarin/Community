﻿using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using miniBBS.Services.GlobalCommands;
using System;
using System.Linq;

namespace miniBBS.Commands
{
    public static class KickUser
    {
        public static void Execute(BbsSession session, string username)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
            {
                bool canDoThis =
                    session.User.Access.HasFlag(AccessFlag.Administrator) ||
                    session.User.Access.HasFlag(AccessFlag.GlobalModerator) ||
                    session.UcFlag.Flags.HasFlag(UCFlag.Moderator);

                if (!canDoThis)
                {
                    session.Io.Error("Access denied");
                    return;
                }

                var targetSession = DI.Get<ISessionsList>().Sessions?.FirstOrDefault(s => true == s.User?.Name?.Equals(username, StringComparison.CurrentCultureIgnoreCase));
                if (targetSession == null)
                {
                    session.Io.Error($"{username} doesn't appear to be online right now.");
                    return;
                }

                if (targetSession.Channel.Name != session.Channel.Name)
                {
                    session.Io.Error($"{username} isn't in {session.Channel.Name}!");
                    return;
                }

                string message = $"{session.User.Name} has kicked {targetSession.User.Name} out of {session.Channel.Name}!";
                
                DI.Get<ILogger>().Log(session, message);
                session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, message));
                
                if (targetSession.Channel.Name.Equals(Constants.DefaultChannelName))
                {
                    targetSession.SetForcedLogout(message);
                }
                else
                {
                    SwitchOrMakeChannel.Execute(targetSession, Constants.DefaultChannelName, allowMakeNewChannel: false);
                }
                
            }
        }
    }
}
