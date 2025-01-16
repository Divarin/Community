using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Models;
using miniBBS.Extensions;
using System;
using System.Linq;

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

            // i=1 to 10 [step (step)] (whitespace optional here)
            if (string.IsNullOrWhiteSpace(line))
                return false;

            string var, strStart, strEnd, strStep;
            strStep = "1";

            // find variable name left of '='
            var parts = line.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return false;
            var = parts[0].Trim();
            line = parts[1]; // everything right of '='

            // find start & end values before & after 'to' (not case sensitive)
            parts = line.Split("to");
            if (parts == null || parts.Length != 2)
                return false;
            strStart = parts[0];
            strEnd = parts[1];

            // see if "step" is specified
            parts = strEnd.Split("step");
            if (parts != null && parts.Length == 2)
            {
                strEnd = parts[0]; // re-assign end to just the end value and not including STEP portion
                strStep = parts[1]; // assign step value
            }

            // try to parse all those ints
            if (!int.TryParse(strStart, out _current))
                throw new RuntimeException("type mismatch for loop starting index");
            if (!int.TryParse(strEnd, out _end))
                throw new RuntimeException("type mismatch for loop ending index");
            if (!int.TryParse(strStep, out _step))
                throw new RuntimeException("type mismatch for STEP");
           
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
