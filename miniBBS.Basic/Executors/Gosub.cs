using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Models;
using miniBBS.Core.Models.Control;

namespace miniBBS.Basic.Executors
{
    public class Gosub : Scoped
    {
        public StatementPointer ReturnToPosition { get; private set; }

        public Gosub()
            : base()
        {
            
        }

        public int Execute(BbsSession session, string line, Variables variables, StatementPointer returnToPosition)
        {
            ReturnToPosition = returnToPosition;

            int gosubLineNum;
            if (int.TryParse(line, out gosubLineNum))
                return gosubLineNum;
            else if (variables.Labels.ContainsKey(line))
                return variables.Labels[line];
            else if (int.TryParse(Evaluate.Execute(session, line, variables), out gosubLineNum))
                return gosubLineNum;
            else
                throw new RuntimeException($"Unable to parse {line} as a line number on GOSUB statement.");            
        }

        public int Execute(int lineNumber, StatementPointer returnToPosition)
        {
            ReturnToPosition = returnToPosition;
            return lineNumber;
        }
    }
}
