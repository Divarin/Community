using miniBBS.Basic.Interfaces;
using miniBBS.Basic.Models;
using miniBBS.Core.Models.Control;

namespace miniBBS.Basic.Executors
{
    public static class Get
    {
        public static void Execute(BbsSession session, string line, Variables variables)
        {
            session.Io.Output("? ");
            var inp = session.Io.InputKey();
            if (!inp.HasValue)
                return;

            string str = inp.Value.ToString();
            session.Io.OutputLine();
            if (string.IsNullOrWhiteSpace(str) && variables.ContainsKey(line))
                variables.Remove(line);
            else if (line.EndsWith("$"))
                variables[line] = '"' + str + '"';
            else
                variables[line] = Evaluate.Execute(str, variables);
        }
    }
}
