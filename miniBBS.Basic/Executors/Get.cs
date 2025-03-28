using miniBBS.Basic.Models;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;

namespace miniBBS.Basic.Executors
{
    public static class Get
    {
        private const char Quote = '"';

        public static void Execute(BbsSession session, string line, Variables variables)
        {
            var inp = session.Io.InputKey();// .InputLine(InputHandlingFlag.ReturnFirstCharacterOnly);

            //session.Io.OutputLine();
            if (line.EndsWith("$"))
                variables[line] = $"{Quote}{inp}{Quote}";
            else
                variables[line] = $"{inp}";
        }
    }
}
