using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Basic.Extensions
{
    public static class StringExtensions
    {
        public static IEnumerable<int> IndexOfAll(this string str, char c)
        {
            for (int i=0; i < str.Length; i++)
            {
                if (str[i] == c)
                    yield return i;
            }
        }

        public static string[] SplitWithStringsIntact(this string str, params char[] delimiters)
        {
            List<string> results = new List<string>();

            StringBuilder builder = new StringBuilder();
            bool inQuotes = false;

            for (int i=0; i < str.Length; i++)
            {
                char c = str[i];
                if (delimiters.Contains(c) && !inQuotes)
                {
                    results.Add(builder.ToString());
                    builder.Clear();
                    continue;
                }
                
                if (c == '"')
                    inQuotes = !inQuotes;

                builder.Append(c);
            }

            if (builder.Length > 0)
                results.Add(builder.ToString());

            return results.ToArray();
        }

        public static string Detokenize(this string str, IDictionary<decimal, string> stringValues)
        {
            if (str.StartsWith("¿[") || str.EndsWith("]¿"))
            {
                str = str.Substring(2);
                str = str.Substring(0, str.Length - 2);
                var token = decimal.Parse(str);
                str = stringValues[token];
                return str;
            }
            return str;
        }

        public static int IndexOf(this string[] array, string element, StringComparison comparison)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].Equals(element, comparison))
                    return i;
            }
            return -1;
        }

        public static string Join(this string[] array, int startIndex, int endIndex, string delimiter)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = startIndex; i <= endIndex; i++)
            {
                if (i > startIndex && delimiter != null)
                    builder.Append(delimiter);
                builder.Append(array[i]);
            }

            return builder.ToString();
        }

        public static int IndexOfClosingParens(this string str, int start)
        {
            int openParens = 0;
            for (int i=start; i < str.Length; i++)
            {
                char c = str[i];
                if (c == '(') 
                    openParens++;
                else if (c == ')')
                {
                    openParens--;
                    if (openParens <= 0)
                        return i;
                }
            }

            return -1;
        }

        public static string Unquote(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;
            str = str.Trim();
            if (str.StartsWith("\""))
                str = str.Substring(1);
            if (str.EndsWith("\""))
                str = str.Substring(0, str.Length - 1);
            return str;
        }
    }
}
