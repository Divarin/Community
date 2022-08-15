using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Models;
using System;

namespace miniBBS.Basic.Executors
{
    public class For : Scoped
    {
        private int _current = 0;
        private int _end;
        private int _step;
        public StatementPointer FirstStatementPosition { get; private set; }

        private Variables _variables;
        public string VariableName { get; private set; }
        private bool _exit = false;

        public For()
            : base()
        {

        }

        public bool Execute(string line, Variables variables, StatementPointer firstStatementPosition)
        {
            FirstStatementPosition = firstStatementPosition;
            _variables = variables;

            // i=1 to 10 [step (step)]
            if (string.IsNullOrWhiteSpace(line))
                return false;
            int pos = line.IndexOf('=');
            string var = line.Substring(0, pos)?.Trim(); // i
            line = line.Substring(pos+1)?.Trim(); // 1 to 10 ...
            pos = line.IndexOf("TO", StringComparison.CurrentCultureIgnoreCase);
            string strStart;
            strStart = line.Substring(0, pos)?.Trim(); // 1

            strStart = Evaluate.Execute(strStart, variables);

            line = line.Substring(pos + 3)?.Trim(); // 10 ...
            int end = line.IndexOf(' ');
            string strEnd;
            if (end < 1)
                strEnd = line?.Trim();
            else
                strEnd = line.Substring(0, end)?.Trim();

            strEnd = Evaluate.Execute(strEnd, variables);

            pos = line.IndexOf("STEP", StringComparison.CurrentCultureIgnoreCase);
            int step = 1;
            if (pos >= 0)
            {
                string strStep = line.Substring(pos + 4);

                strStep = Evaluate.Execute(strStep, variables);

                if (!int.TryParse(strStep, out step))
                {
                    throw new RuntimeException("type mismatch for STEP");
                }                
            }

            if (!int.TryParse(strStart, out _current))
                throw new RuntimeException("type mismatch for loop starting index");
            if (!int.TryParse(strEnd, out _end))
                throw new RuntimeException("type mismatch for loop ending index");

            _step = step;
                        
            VariableName = var;
            if (VariableName.StartsWith("_"))
                LocalVariables[VariableName] = _current.ToString();
            else
                _variables[VariableName] = _current.ToString();
            
            return true;
        }

        public bool Advance(string args)
        {
            if (_exit)
                return false;

            if (!string.IsNullOrWhiteSpace(args) && !args.Equals(VariableName, StringComparison.CurrentCultureIgnoreCase))
                throw new RuntimeException($"hit NEXT {args} while in FOR {VariableName} loop.");

            string v = VariableName.StartsWith("_") ? LocalVariables[VariableName] : _variables[VariableName];
            int i;
            if (int.TryParse(v, out i))
                _current = i;

            int next = _current + _step;
            if (_step < 0 && next >= _end)
            {
                _current = next;
                if (VariableName.StartsWith("_"))
                    LocalVariables[VariableName] = _current.ToString();
                else
                    _variables[VariableName] = _current.ToString();
                return true;
            }
            else if (_step >= 0 && next <= _end)
            {
                _current = next;
                if (VariableName.StartsWith("_"))
                    LocalVariables[VariableName] = _current.ToString();
                else
                    _variables[VariableName] = _current.ToString();
                return true;
            }
            return false;
        }

        public void Exit()
        {
            _exit = true;
        }
    }
}
