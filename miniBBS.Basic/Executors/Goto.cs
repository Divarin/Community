using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Models;

namespace miniBBS.Basic.Executors
{
    public static class Goto
    {
        public static int Execute(string line, Variables variables)
        {
            int i;
            if (int.TryParse(line, out i))
                return i;
            else if (true == variables.Labels?.ContainsKey(line))
                return variables.Labels[line];
            else if (int.TryParse(Evaluate.Execute(line, variables), out i))
                return i;

            throw new RuntimeException($"Invalid GOTO: {line}");

        }
    }
}
