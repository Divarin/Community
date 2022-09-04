using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using System;
using System.Linq;

namespace miniBBS.Commands
{
    public static class SystemControl
    {
        public static void Execute(BbsSession session, params string[] args)
        {
            if (!session.User.Access.HasFlag(AccessFlag.Administrator))
                return;

            if (args == null || args.Length < 1)
            {
                session.Io.OutputLine($"Current flags: {Program.SysControl}");
                var possibleFlags = from SystemControlFlag f in Enum.GetValues(typeof(SystemControlFlag))
                                    select f.ToString();
                session.Io.OutputLine($"Possible flags:{Environment.NewLine}{string.Join(Environment.NewLine, possibleFlags)}");
                return;
            }

            if (Enum.TryParse(args[0], true, out SystemControlFlag flag))
            {
                if (Program.SysControl.HasFlag(flag))
                {
                    Program.SysControl &= ~flag;
                    session.Io.OutputLine($"Removed flag: {flag}");
                }
                else
                {
                    Program.SysControl |= flag;
                    session.Io.OutputLine($"Added flag: {flag}");
                }
            }
        }
    }
}
