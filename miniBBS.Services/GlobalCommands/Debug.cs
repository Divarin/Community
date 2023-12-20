using miniBBS.Core.Models.Control;

namespace miniBBS.Services.GlobalCommands
{
    public static class Debug
    {
        public static void Execute(BbsSession session, params string[] args)
        {
            if (args?.Length >= 1 && byte.TryParse(args[0], out var b))
            {
                session.Io.OutputRaw(new[] { b });
            }
            else
            {
                session.Io.Output("Type a character: ");
                var bytes = session.Io.InputRaw();
                session.Io.OutputLine();
                session.Io.OutputLine($"I received a total of {bytes.Length} bytes.");
                for (int i = 0; i < bytes.Length; i++)
                {
                    session.Io.OutputLine($"#{i + 1} : {(int)bytes[i]}");
                }
            }
        }
    }
}
