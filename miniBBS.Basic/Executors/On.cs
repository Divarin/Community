using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Models;
using System;
using System.Linq;

namespace miniBBS.Basic.Executors
{
    public static class On
    {
        public static OnResult Execute(string line, Variables variables)
        {
            OnResult result = new OnResult()
            {
                Success = false,
                Gosub = false
            };

            // (expression) GOTO/GOSUB (line number list)
            line = line.ToLower();
            int pos = line.IndexOf("goto");
            int lineNumbersStart = pos + 5;

            if (pos <= 0)
            {
                pos = line.IndexOf("gosub");
                if (pos > 0)
                {
                    result.Gosub = true;
                    lineNumbersStart = pos + 6;
                }
            }

            if (pos > 0)
            {
                string exp = line.Substring(0, pos).Trim();
                string[] gotos = line
                    .Substring(lineNumbersStart, line.Length - lineNumbersStart)
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                var expressionValue = Evaluate.Execute(exp, variables);
                if (int.TryParse(expressionValue, out int v))
                {
                    if (v>=1 && v<= gotos.Length)
                    {
                        result.LineNumber = GetLineNumber(gotos[v - 1], variables);
                        result.Success = true;
                    }
                }
            }

            return result;
        }

        private static int GetLineNumber(string val, Variables variables)
        {
            if (val.StartsWith("!") && val.Length > 1)
                val = val.Substring(1);

            int lineNum;
            if (int.TryParse(val, out lineNum))
                return lineNum;
            else if (variables.Labels.ContainsKey(val))
                return variables.Labels[val];
            else if (int.TryParse(Evaluate.Execute(val, variables), out lineNum))
                return lineNum;
            else
                throw new RuntimeException($"Unable to parse {val} as a line number on GO statement.");
        }

        public struct OnResult
        {
            public bool Success { get; set; }
            public bool Gosub { get; set; }
            public int LineNumber { get; set; }
        }
    }
}
