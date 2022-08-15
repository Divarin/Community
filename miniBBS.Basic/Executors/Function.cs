using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Basic.Executors
{
    public class Function
    {
        public static Function Create(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                throw new RuntimeException("illegal function definition");

            Function function = new Function();

            // name(v1,v2,...)=expression
            function.Name = ParseFunctionName(line);
            function._variableNames = ParseVariableNames(line);
            function._expression = ParseExpression(line);

            return function;
        }

        private static string ParseFunctionName(string line)
        {
            int pos = line.IndexOf("(");
            if (pos <= 0)
                throw new RuntimeException("illegal function definition");
            string name = line.Substring(0, pos);
            if (!name.StartsWith("fn", StringComparison.CurrentCultureIgnoreCase))
                throw new RuntimeException("function name must start with 'FN'");
            return name;
        }

        private static IList<char> ParseVariableNames(string line)
        {
            int pos = line.IndexOf("(");
            if (pos <= 0)
                throw new RuntimeException("illegal function definition");
            pos++;
            int end = line.IndexOf(")", pos);
            if (end <= pos)
                throw new RuntimeException("illegal function definition");
            int len = end - pos;
            string s = line.Substring(pos, len);
            return s.Split(new char[] { ',', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x =>
                {
                    string varb = x.Trim();
                    if (varb.Length != 1 || !char.IsLetter(varb[0]))
                        throw new RuntimeException($"illegal variable name in function definition: {varb}.  Function variables must be only a single letter.");
                    return varb[0];
                })
                .ToList();
        }

        private static string ParseExpression(string line)
        {
            int pos = line.IndexOf("=");
            if (pos <= 0 || pos == line.Length-1)
                throw new RuntimeException("illegal function definition");
            return line.Substring(pos + 1).Trim();
        }

        private IList<char> _variableNames = new List<char>();
        private string _expression;
        public string Name { get; private set; }

        public double Execute(string line, Variables variables)
        {
            // split line into variable values, each should be a number
            if (string.IsNullOrWhiteSpace(line) && _variableNames.Count > 0)
                throw new RuntimeException($"function {Name} requires {_variableNames.Count} values");

            if (line.StartsWith("("))
                line = line.Substring(1);
            if (line.EndsWith(")"))
                line = line.Substring(0, line.Length - 1);

            var values = line
                .Split(new char[] { ',', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x =>
                {
                    x = Evaluate.Execute(x, variables);
                    double d;
                    if (!double.TryParse(x, out d))
                        throw new RuntimeException($"type mismatch calling function {Name}, value {x} is not a number");
                    return d;
                })
                .ToArray();

            if (values.Length != _variableNames.Count)
                    throw new RuntimeException($"function {Name} requires {_variableNames.Count} values");

            // apply variable values to appropriate variables in the expression
            StringBuilder builder = new StringBuilder();
            for (int i=0; i < _expression.Length; i++)
            {
                char c = _expression[i];
                if (char.IsLetter(c))
                {
                    bool prevIsLetter = i > 0 && char.IsLetter(_expression[i - 1]);
                    bool nextIsLetter = i < _expression.Length - 1 && char.IsLetter(_expression[i + 1]);
                    if (!prevIsLetter && !nextIsLetter && _variableNames.Contains(c))
                    {
                        int valueIndex = _variableNames.IndexOf(c);
                        double val = values[valueIndex];
                        builder.Append(val);
                        continue;
                    }
                }

                builder.Append(c);
            }

            // evaluate expression and return result
            var expression = builder.ToString();
            var value = Evaluate.Execute(expression, variables);
            double result;
            if (double.TryParse(value, out result))
                return result;
            throw new RuntimeException($"function evaluation failed.  Result is non-numeric: {result}");
        }
    }
}
