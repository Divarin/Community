using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Interfaces;
using miniBBS.Basic.Models;
using miniBBS.Core.Models.Control;
using System;
using System.Linq;

namespace miniBBS.Basic.Executors
{
    public static class Let
    {
        public static void Execute(BbsSession session, string statement, Variables variables, string rootDirectory)
        {
            int pos = statement.IndexOf('=');
            if (pos > 0)
            {
                string variableName = statement.Substring(0, pos)?.Trim();

                if (string.IsNullOrEmpty(variableName))
                    throw new RuntimeException("empty variable name");
                if (variableName[0] != '_' && !char.IsLetter(variableName[0]))
                    throw new RuntimeException("variable name must start with a letter");
                if (variableName.Any(c => char.IsWhiteSpace(c)))
                    throw new RuntimeException("invalid whitespace in variable name");

                string value = statement.Substring(pos+1)?.Trim();

                if ("inkey$".Equals(variableName, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(value) || value == "\"\"")
                    {
                        session.Io.ClearPolledKey();
                        return;
                    }
                    else
                        throw new RuntimeException("cannot set INKEY$ directly.");
                }

                if (string.IsNullOrEmpty(value) && variables.ContainsKey(variableName))
                {
                    if (variableName[0] == '_')
                        TryRemoveScopedVariable(variableName, variables);
                    else
                        variables.Remove(variableName);
                }
                else
                {
                    bool quote = 
                        variableName.Contains("$") ||
                        (value.StartsWith("\"") && value.EndsWith("\""));

                    value = Evaluate.Execute(value, variables);

                    if (value.StartsWith("@") && value.Length > 1)
                    {
                        new Sql(rootDirectory).Execute(session, '"' + value.Substring(1) + '"', variables, variableName);
                        return;
                    }

                    if (quote)
                        value = '"' + value + '"';

                    if (variableName[0] == '_')
                        TryAssignScopedVariable(variableName, value, variables);
                    else
                        variables[variableName] = value;
                }
            }
        }

        private static void TryRemoveScopedVariable(string variableName, Variables variables)
        {
            IScoped scoped = variables.PeekScoped();
            if (scoped != null && scoped.LocalVariables.ContainsKey(variableName))
                scoped.LocalVariables.Remove(variableName);
        }

        private static void TryAssignScopedVariable(string variableName, string value, Variables variables)
        {
            IScoped scoped = variables.PeekScoped();
            if (scoped != null)
                scoped.LocalVariables[variableName] = value;
        }
    }
}
