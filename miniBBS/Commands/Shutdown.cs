using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;

namespace miniBBS.Commands
{
    public static class Shutdown
    {
        public static void Execute(BbsSession session)
        {
            if (session.SysControl.HasFlag(SystemControlFlag.Shutdown))
            {
                session.Io.OutputLine("System shutdown aborted.");
                session.SysControl &= ~SystemControlFlag.Shutdown;
            }
            else
            {
                session.Io.OutputLine("System shutdown flag set, when you log out the system will shut down.  Use this command again to undo.");
                session.SysControl |= SystemControlFlag.Shutdown;
            }
        }
    }
}
