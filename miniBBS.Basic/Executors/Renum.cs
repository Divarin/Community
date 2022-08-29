using miniBBS.Basic.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Basic.Executors
{
    public static class Renum
    {
        private static readonly string[] _remappedStatements = new string[] { "goto", "gosub" };

        public static SortedList<int,string> Execute(string line, SortedList<int, string> progLines)
        {
            int start = 10;
            int step = 10;
            if (!string.IsNullOrWhiteSpace(line))
            {
                if (!int.TryParse(line, out start) && line.Contains(','))
                {
                    var parts = line.Split(',');
                    if (parts?.Length != 2 || !int.TryParse(parts[0], out start) || !int.TryParse(parts[1], out step))
                        throw new RuntimeException("invalid paramters for renum command");
                }
            }

            SortedList<int, string> result = new SortedList<int, string>(progLines.Count);
            
            // [old line number] = new line number
            Dictionary<int, int> lineNumMap = new Dictionary<int, int>();
            int current = start;
            foreach (var l in progLines)
            {
                int oldLineNumber = l.Key;
                string statement = l.Value;
                int newLineNumber = current;
                lineNumMap[oldLineNumber] = newLineNumber;
                result.Add(newLineNumber, statement);
                current += step;
            }

            int[] newLineNumbers = result.Keys.ToArray();
            // look for GOTO n and GOSUB n
            for (int l=0; l < newLineNumbers.Length; l++)
            {
                int newLineNumber = newLineNumbers[l];
                string statement = result[newLineNumber];

                var parts = statement.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                bool remapped = false;
                for (int i=0; i < parts.Length-1; i++)
                {
                    foreach (var rms in _remappedStatements)
                    {
                        if (parts[i].Equals(rms, StringComparison.CurrentCultureIgnoreCase) || // "goto"
                            parts[i].EndsWith(rms, StringComparison.CurrentCultureIgnoreCase)) // "some stuff:goto"
                        {
                            //int oldLineNumber;
                            // accomodate ON x GOTO and ON x GOSUB by splitting out line number by commas and processing each one
                            var renumbered = parts[i + 1]
                                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(x =>
                                {
                                    if (int.TryParse(x, out int _i))
                                        return new int?(_i);
                                    return null;
                                })
                                .Where(x => x.HasValue && lineNumMap.ContainsKey(x.Value))
                                .Select(x => lineNumMap[x.Value])
                                .ToArray();
                            if (true == renumbered?.Any())
                            {
                                parts[i + 1] = string.Join(",", renumbered);
                                remapped = true;
                            }

                            //if (int.TryParse(parts[i+1], out oldLineNumber) && lineNumMap.ContainsKey(oldLineNumber))
                            //{
                            //    parts[i + 1] = lineNumMap[oldLineNumber].ToString();
                            //    remapped = true;
                            //}
                        }
                    }
                }
                if (remapped)
                    result[newLineNumber] = string.Join(" ", parts);
            }


            return result;
        }
    }
}
