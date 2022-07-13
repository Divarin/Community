using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Returns a string which is lowercase except for the first character which is uppercase
        /// </summary>
        public static string ToUpperFirst(this string str)
        {
            char[] arr = new char[str.Length];
            arr[0] = char.ToUpper(str[0]);
            for (int i = 1; i < str.Length; i++)
                arr[i] = char.ToLower(str[i]);
            string result = new string(arr, 0, arr.Length);
            return result;
        }

        public static string Repeat(this char c, int numRepeats)
        {
            char[] arr = new char[numRepeats];
            for (int i = 0; i < numRepeats; i++)
                arr[i] = c;
            return new string(arr, 0, arr.Length);
        }

        public static IEnumerable<string> SplitAndWrap(this string str, BbsSession session, OutputHandlingFlag flags)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                yield return str;
                yield break;
            }

            int start = 0;
            int col = 0;
            int breakIndex = 0;
            bool inAnsiCode = false;

            for (int i=0; i < str.Length; i++)
            {
                char c = str[i];

                // Try to not increment (col)umn number if we're inside of an ansi sequence since those characters 
                // aren't actually displayed.
                if (c == '\u001b')
                    inAnsiCode = true;
                else if (inAnsiCode && c == 'm')
                    inAnsiCode = false;
                else if (c == Constants.InlineColorizer)
                    inAnsiCode = !inAnsiCode;
                else if (!inAnsiCode)
                    col++;

                if (c != 13 && c != 10 && char.IsWhiteSpace(c))
                    breakIndex = i;

                // If CBM use Cols-1 because if we print a character on the last column then that will shift the cursor
                // down to the next line, even though we haven't reached a newline character.
                var maxCols = session.Cols;
                if (session.Io.EmulationType == TerminalEmulation.Cbm)
                    maxCols--;

                if (c == 13 || col >= maxCols || i==str.Length-1)
                {
                    int end = breakIndex > start ? breakIndex : i;
                    if (i == str.Length - 1)
                        end = str.Length - 1;
                    else if (c == 13)
                        end = i;

                    int len = end - start + 1;
                    string substring = str.Substring(start, len);
                    if (!string.IsNullOrWhiteSpace(substring))
                    {
                        substring = substring
                            .TrimEnd(' ')
                            .Replace("\r\0", "\r");

                        if (substring[substring.Length - 1] != 13 && substring[substring.Length - 1] != 10)
                            substring += Environment.NewLine;
                        else if (substring[substring.Length - 1] == 13)
                            substring += "\n";
                        if (substring.Length > 2 && substring[0] == 13 && substring[1] == 10)
                            substring = substring.Substring(2);
                        if (!string.IsNullOrWhiteSpace(substring))
                        {
                            if (!flags.HasFlag(OutputHandlingFlag.DoNotTrimStart))
                                substring = substring.TrimStart(' ');
                            yield return substring.Replace(Constants.Spaceholder, ' ');
                        }
                    }
                    else 
                        yield return Environment.NewLine;

                    start = end;
                    i = start;
                    col = 0;
                }
            }
        }

        public static string MaxLength(this string str, int maxLength, bool addElipsis = true)
        {
            if (string.IsNullOrWhiteSpace(str) || str.Length <= maxLength)
                return str;
            if (addElipsis)
                return str.Substring(0, maxLength) + "...";
            else
                return str.Substring(0, maxLength);
        }

        public static bool IsPrintable(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return false;
            if (str.Any(c => char.IsDigit(c) || char.IsLetter(c) || char.IsPunctuation(c)))
                return true;
            return false;
        }
    }
}
