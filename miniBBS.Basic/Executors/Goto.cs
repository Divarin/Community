using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Models;
using miniBBS.Core.Models.Control;

namespace miniBBS.Basic.Executors
{
    public static class Goto
    {
        public static int Execute(BbsSession session, string line, Variables variables)
        {
            int i;
            if (int.TryParse(line, out i))
                return i;
            else if (true == variables.Labels?.ContainsKey(line))
                return variables.Labels[line];
            else if (int.TryParse(Evaluate.Execute(session, line, variables), out i))
                return i;

            throw new RuntimeException($"Invalid GOTO: {line}");

        }
    }
}
