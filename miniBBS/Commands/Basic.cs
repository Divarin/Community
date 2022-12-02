using miniBBS.Basic.Models;
using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class Basic
    {
        private static readonly Dictionary<string, Func<string>> _environmentVariables = new Dictionary<string, Func<string>>();
        private static readonly Variables _variables = new Variables(_environmentVariables);

        public static void Execute(BbsSession session, params string[] args)
        {
            if (true != args?.Any())
            {
                PrintUsage(session);
                return;
            }

            var line = string.Join(" ", args);
            if (!line.StartsWith("let", StringComparison.CurrentCultureIgnoreCase) &&
                !line.StartsWith("print", StringComparison.CurrentCultureIgnoreCase) &&
                !line.StartsWith("?", StringComparison.CurrentCultureIgnoreCase) &&
                line.Count(c => c == '=') >= 1)
            {
                line = "let " + line;
                args = line.Split(' ');
            }

            switch (args.First().ToLower())
            {
                case "let":
                    Let(session, string.Join(" ", args.Skip(1)));
                    break;
                case "print":
                case "?":
                    Print(session, string.Join(" ", args.Skip(1)));
                    break;
                case "vars":
                    ShowVars(session);
                    break;
                default:
                    PrintUsage(session);
                    break;
            }
        }

        private static void ShowVars(BbsSession session)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Global Basic Variables");
            foreach (var v in _variables.Keys)
            {
                builder.AppendLine($"{v} = {_variables[v]}");
            }

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                session.Io.Output(builder.ToString());
        }

        private static void Print(BbsSession session, string expr)
        {
            string result;
            try
            {
                result = miniBBS.Basic.Executors.Evaluate.Execute(expr, _variables);
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                var msg = $"{session.User.Name} prints the Basic expression:{Environment.NewLine}{expr}{Environment.NewLine}{result.Color(ConsoleColor.Green)}";
                session.Io.OutputLine(msg);
                session.Messager.Publish(session, new GlobalMessage(session.Id, msg));
            }
        }

        private static void Let(BbsSession session, string expr)
        {
            var parts = expr?.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            if (true != parts?.Any())
                return;
            var variableName = parts[0];
            var variableValue = parts.Length >= 2 ? parts[1] : null;

            string msg = null;
            if (!string.IsNullOrWhiteSpace(variableValue))
                msg = $"{session.User.Name} sets Basic variable '{variableName}' to '{variableValue}'.";
            else if (_variables.ContainsKey(variableName))
                msg = $"{session.User.Name} removes Basic variables '{variableName}'.";

            miniBBS.Basic.Executors.Let.Execute(session, expr, _variables, null);

            if (!string.IsNullOrWhiteSpace(msg))
            {
                session.Messager.Publish(session, new GlobalMessage(session.Id, msg));
                using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                    session.Io.OutputLine(msg);
            }
        }

        private static void PrintUsage(BbsSession session)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Basic Evaluate Usage:");
            builder.AppendLine("/basic let var=val : sets a variable (var) to a value (val)");
            builder.AppendLine($"{Constants.Spaceholder.Repeat(3)}/basic let a$=\"foo\" : sets a$ to \"foo\"");
            builder.AppendLine($"{Constants.Spaceholder.Repeat(3)}/basic let x=3 : sets x to 3");
            builder.AppendLine($"{Constants.Spaceholder.Repeat(3)}/basic x=3 : 'let' keyword may be omitted.");
            builder.AppendLine("/basic print expr : evaluate and prints the expression");
            builder.AppendLine($"{Constants.Spaceholder.Repeat(3)}/basic print 22/7 : calculates 22/7 and prints the result");
            builder.AppendLine($"{Constants.Spaceholder.Repeat(3)}/basic print a$ : prints the value of a$");
            builder.AppendLine($"{Constants.Spaceholder.Repeat(3)}/basic ? a$ : '?' may be used in place of the keyword 'print'");
            builder.AppendLine("/basic vars : prints all current variables");

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
                session.Io.Output(builder.ToString());
        }
    }
}
