using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using System;
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

                if (session.User.Access.HasFlag(AccessFlag.Administrator))
                {
                    var metas = DI.GetRepository<Metadata>()
                        .Get(m => m.UserId, user.Id)
                        .Where(m => !_filteredMetadataTypes.Contains(m.Type));

                    foreach (var meta in metas)
                        builder.AppendLine($"Meta         : {meta.Type} = {meta.Data}");
                }

                session.Io.OutputLine(builder.ToString());

            }
        }
    }
}
