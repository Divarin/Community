using miniBBS.Basic.Extensions;
using miniBBS.Basic.Models;
using System;
using System.Linq;

namespace miniBBS.Basic.Executors
{
    public static class If
    {
        /// <summary>
        /// Returns the statement portion (the THEN) if the condition is true, otherwise returns null
        /// </summary>
        public static string Execute(string line, Variables variables)
        {
            const string then = "THEN";
            // if condition then statement
            string condition;
            string statement;

            var parts = line.SplitWithStringsIntact(' ');
            int thenPos = parts.IndexOf(then, StringComparison.CurrentCultureIgnoreCase);
            //int pos = line.IndexOf(then, StringComparison.CurrentCultureIgnoreCase);
            if (thenPos > 0)
            {
                condition = parts.Join(0, thenPos - 1, " ");
                statement = parts.Join(thenPos + 1, parts.Length - 1, " ");

                //condition = line.Substring(0, pos)?.Trim();
                string b;
                bool? variableExists = CheckingVariableIsDefined(condition, variables);
                if (variableExists.HasValue)
                    b = variableExists.Value ? "1" : "0";
                else
                    b = Evaluate.Execute(condition, variables);

                if ("1".Equals(b))
                {
                    //statement = line.Substring(pos + then.Length)?.Trim();
                    int lineNumber;
                    if (!string.IsNullOrWhiteSpace(statement) && int.TryParse(statement, out lineNumber))
                        statement = $"GOTO {lineNumber}";
                    return statement;
                }
            }

            return null;
        }

        private static bool? CheckingVariableIsDefined(string condition, Variables variables)
        {
            string variableName = null;

            if (condition.StartsWith("defined ", StringComparison.CurrentCultureIgnoreCase))
            {
                variableName = condition.Substring(8).Replace(" ", "");
                variableName = variables.EvaluateArrayExpressions(variableName);
                return variables.Keys.Any(k => k.Equals(variableName, StringComparison.CurrentCultureIgnoreCase));
            }
            else if (condition.StartsWith("not defined ", StringComparison.CurrentCultureIgnoreCase))
            {
                variableName = condition.Substring(12).Trim();
                variableName = variables.EvaluateArrayExpressions(variableName);
                return !variables.Keys.Any(k => k.Equals(variableName, StringComparison.CurrentCultureIgnoreCase));
            }

            condition = condition.Replace(" ", "");
            int pos = condition.IndexOf("<>\"\"");
            if (pos > 0)
            {
                variableName = condition.Substring(0, pos);
                if (variables.Keys.Any(k => k.Equals(variableName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    string value = variables[variableName];
                    return !string.IsNullOrEmpty(value) && value != "\"\"";
                }
                else
                    return false;
            }
            else
            {
                pos = condition.IndexOf("=\"\"");
                if (pos > 0)
                {
                    variableName = condition.Substring(0, pos);
                    if (variables.Keys.Any(k => k.Equals(variableName, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        string value = variables[variableName];
                        return string.IsNullOrEmpty(value) || value == "\"\"";
                    }
                    else
                        return true;
                }
            }

            return null;
        }

    }
}
