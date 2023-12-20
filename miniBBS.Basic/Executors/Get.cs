using miniBBS.Basic.Models;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;

namespace miniBBS.Basic.Executors
{
    public static class Get
    {
        public static void Execute(BbsSession session, string line, Variables variables)
        {
            //session.Io.Output("? ");
            var inp = session.Io.InputLine(InputHandlingFlag.ReturnFirstCharacterOnly);
            
            session.Io.OutputLine();
            if (line.EndsWith("$"))
                variables[line] = '"' + inp + '"';
            else
                variables[line] = inp;
        }
    }
}
