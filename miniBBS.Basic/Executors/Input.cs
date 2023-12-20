using miniBBS.Basic.Models;
using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Basic.Executors
{
    public static class Input
    {
        public static void Execute(BbsSession session, string line, Variables variables)
        {
            InputParameters p = ParseInputParameters(line, variables);
            if (!string.IsNullOrWhiteSpace(p.Question))
                session.Io.Output(p.Question);

            //session.Io.Output("? ");
            string inp = session.Io.InputLine();
            session.Io.OutputLine();
            if (inp == null)
                inp = "";

            var values = inp.Split(',');

            for (int i=0; i < p.VariableNames.Length && i < values.Length; i++)
            {
                string varb = p.VariableNames[i];
                string value = values[i];

                if (varb.EndsWith("$"))
                    variables[varb] = "\"" + value + "\"";
                else
                    variables[varb] = value;
            }
        }

        private static InputParameters ParseInputParameters(string line, Variables variables)
        {
            InputParameters p = new InputParameters();
            int pos = line.IndexOf(';');
            if (pos > 0)
            {
                string question = line.Substring(0, pos);
                question = Evaluate.Execute(question, variables);
                p.Question = question;
                line = line.Substring(pos + 1);
            }

            var varbs = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            p.VariableNames = varbs;
            return p;
        }

        private struct InputParameters
        {
            public string Question { get; set; }
            public string[] VariableNames { get; set; }
        }
    }
}
