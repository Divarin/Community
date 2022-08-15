using miniBBS.Basic.Interfaces;
using miniBBS.Basic.Models;
using miniBBS.Core.Models.Control;

namespace miniBBS.Basic.Executors
{
    public static class Print
    {
        
        public static void Execute(BbsSession session, string line, Variables variables)
        {
            if (line == null)
                line = string.Empty;

            line = line.Trim();
            bool newlineAfter = true;
            if (line.EndsWith(";"))
            {
                newlineAfter = false;
                line = line.Substring(0, line.Length - 1);
            }
            line = Evaluate.Execute(line, variables);

            if (newlineAfter)
                session.Io.OutputLine(line);
            else
                session.Io.Output(line);

        }



    }
}
