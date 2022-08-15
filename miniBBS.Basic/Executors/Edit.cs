using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Models;
using miniBBS.Core.Models.Control;
using System.Collections.Generic;

namespace miniBBS.Basic.Executors
{
    public static class Edit
    {
        public static EditResult Execute(BbsSession session, string line, SortedList<int, string> progLines)
        {
            EditResult result = new EditResult();
            result.Aborted = true;

            string search, replace;
            search = replace = null;

            var parts = line.Split(' ');
            if (parts?.Length < 1)
                throw new RuntimeException("invalid number of arguments for Edit command.");

            int ln;
            if (!int.TryParse(parts[0], out ln))
                throw new RuntimeException("type mismatch, 'Edit' requirees a line number as the first argument.");

            if (parts.Length == 3)
            {
                if (!progLines.ContainsKey(ln))
                    throw new RuntimeException($"line {ln} does not exist.");

                search = parts[1];
                replace = parts[2];
            }
            else
            {
                session.Io.Output("Part to replace: ");
                search = session.Io.InputLine();
                session.Io.OutputLine();

                session.Io.Output("Replace with: ");
                replace = session.Io.InputLine();
                session.Io.OutputLine();
            }

            result.LineNumber = ln;
            result.OriginalLine = progLines[ln];

            if (!string.IsNullOrWhiteSpace(search) && !string.IsNullOrWhiteSpace(replace) && result.OriginalLine.Contains(search))
            {
                result.Aborted = false;
                result.NewLine = result.OriginalLine.Replace(search, replace);
            }

            return result;
        }

    }
}
