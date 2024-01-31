using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Models;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using miniBBS.Services;
using System;
using System.Linq;

namespace miniBBS.Basic.Executors
{
    public static class Notify
    {
        internal static void Execute(BbsSession session, string args, Variables variables)
        {
            if (string.IsNullOrWhiteSpace(args))
                throw new RuntimeException("Syntax error for notify, requires username and message");
            var pos = args.IndexOf(' ');
            if (pos < 1)
                throw new RuntimeException("Syntax error for notify, requires username and message");
            var username = args.Substring(0, pos).Trim();
            var message = args.Substring(pos).Trim();
            message = Evaluate.Execute(message, variables);
            var userId = session.Usernames.FirstOrDefault(x => x.Value.Equals(username, StringComparison.CurrentCultureIgnoreCase)).Key;
            if (userId == default)
                throw new RuntimeException($"Notify error, user '{username}' not found");

            var di = GlobalDependencyResolver.Default;

            var userIsOnline =
                di.Get<ISessionsList>()
                .Sessions
                .Any(s => s.User?.Id == userId);

            if (userIsOnline)
                di.Get<IMessager>().Publish(session, new UserMessage(session.Id, userId, message));
            else
                di.Get<INotificationHandler>().SendNotification(userId, message);
        }
    }
}
