using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Commands
{
    public static class IpBan
    {
        public static void Execute(BbsSession session, ref List<string> ipBans, params string[] args)
        {
            if (!session.User.Access.HasFlag(AccessFlag.Administrator))
            {
                session.Io.Error("Access denied.");
                return;
            }

            bool remove = true == args?.FirstOrDefault()?.StartsWith("r", StringComparison.CurrentCultureIgnoreCase);

            var mask = remove ? string.Join(" ", args) : string.Join(" ", args.Skip(1));

            if (!string.IsNullOrWhiteSpace(mask))
            {
                var repo = DI.GetRepository<Core.Models.Data.IpBan>();

                if (remove)
                {
                    var existing = repo.Get().Where(x => FitsMask(x.IpMask, mask)).ToArray();
                    if (true == existing?.Any())
                    {
                        session.Io.OutputLine($"Removing IP Bans:{Environment.NewLine}{string.Join(Environment.NewLine, existing.Select(x => x.IpMask))}");
                        foreach (var e in existing)
                        {
                            if (ipBans.Contains(e.IpMask))
                                ipBans.Remove(e.IpMask);
                        }
                        repo.DeleteRange(existing);
                    }
                }
                else
                {
                    repo.Insert(new Core.Models.Data.IpBan
                    {
                        IpMask = mask
                    });
                    ipBans.Add(mask);
                    session.Io.OutputLine($"Added '{mask}' to IP ban list.");
                }
            }
        }

        public static bool FitsMask(string ip, string mask)
        {
            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(mask))
                return false;
            var ipParts = ip.Split('.');
            var maskParts = mask.Split('.');

            bool fits = true;
            for (int i = 0; fits && i < ipParts.Length && i < maskParts.Length; i++)
            {
                var ipPart = ipParts[i];
                var maskPart = maskParts[i];
                fits &= ipPart == maskPart || maskPart == "*";
            }

            return fits;
        }
    }
}
