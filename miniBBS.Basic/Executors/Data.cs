using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Extensions;
using miniBBS.Basic.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Basic.Executors
{
    public class Data
    {
        private List<string> _values = new List<string>();
        private int _readPointer = 0;

        public void Read(string line, Variables variables)
        {
            if (_readPointer >= _values.Count)
                throw new RuntimeException("out of data");

            var varbs = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var varb in varbs)
            {
                string value = _values[_readPointer++];
                value = Evaluate.Execute(value, variables);
                variables[varb] = value;
            }
        }

        public void Restore()
        {
            _readPointer = 0;
        }

        public static Data CreateFromDataStatements(IEnumerable<string> programLines)
        {
            Data data = new Data();

            if (true == programLines?.Any())
            {
                var values = programLines
                    .SelectMany(l => l.SplitWithStringsIntact(':'))
                    .Where(s => s.StartsWith("data ", StringComparison.CurrentCultureIgnoreCase))
                    .Select(s => s.Substring(5))
                    .SelectMany(s => s.SplitWithStringsIntact(','))
                    .ToArray();

                if (values.Length > 0)
                    data._values.AddRange(values);
            }

            return data;
        }
    }
}
