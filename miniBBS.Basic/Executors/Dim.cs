using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Models;
using miniBBS.Core.Models.Control;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Basic.Executors
{
    public static class Dim
    {
        public static void Execute(BbsSession session, string variablesName, Variables variables)
        {
            string defaultValue = variablesName.Contains("$") ? "\"\"" : "0";
            if (variablesName.Contains("("))
                DimArray(session, variablesName, defaultValue, variables);
            else
                Let.Execute(session, $"{variablesName}={defaultValue}", variables, null);
        }

        private static void DimArray(BbsSession session, string variablesName, string defaultValue, Variables variables)
        {
            var indicies = GetIndicies(variablesName, variables);
            var arrayName = variablesName
                .Substring(0, variablesName.IndexOf("("))
                .Trim();

            var list = new List<string>();
            for (var d=indicies.Length-1; d >=0; d--)
            {
                GetSubs(indicies[d], ref list);
            }
            var ccount = list.Max(x => x.Count(c => c == ','));            
            list = list.Where(x => x.Count(c => c == ',') == ccount).ToList();

            foreach (var item in list)
            {
                Let.Execute(session, $"{arrayName}({item})={defaultValue}", variables, null);
            }
        }

        private static void GetSubs(int count, ref List<string> list)
        {
            if (list.Count == 0)
            {
                for (var i = 0; i < count; i++)
                    list.Add(i.ToString());
            }
            else
            {
                var end = list.Count;
                for (var x=0; x < end; x++)
                {
                    var item = list[x];
                    for (var i = 0; i < count; i++)
                        list.Add($"{i},{item}");
                }
            }
        }

        private static int[] GetIndicies(string variablesName, Variables variables)
        {
            var start = variablesName.IndexOf('(');
            var end = variablesName.IndexOf(')', start);
            if (end <= start)
                throw new RuntimeException("Cannot DIM with empty index value(s)");
            start++;
            var len = end - start;
            var substr = variablesName.Substring(start, len);
            var indicies = substr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var results = new List<int>();
            foreach (var index in indicies.Select(x => Evaluate.Execute(x, variables)))
            {
                if (int.TryParse(index, out var i))
                    results.Add(i);
                else
                    throw new RuntimeException($"Cannot DIM using index value '{index}'");
            }
            return results.ToArray();
        }
    }
}
