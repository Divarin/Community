using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace miniBBS.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Returns a string which is lowercase except for the first character which is uppercase
        /// </summary>
        public static string ToUpperFirst(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;

            char[] arr = new char[str.Length];
            arr[0] = char.ToUpper(str[0]);
            for (int i = 1; i < str.Length; i++)
                arr[i] = char.ToLower(str[i]);
            string result = new string(arr, 0, arr.Length);
            return result;
        }

        public static string Repeat(this string str, int count)
        {
            if (str == null || count == 1)
                return str;

            char[] array = new char[str.Length * count];
            int offset = 0;

            for (int i = 0; i < count; i++)
            {
                for (int c = 0; c < str.Length; c++)
                {
                    array[offset++] = str[c];
                }
            }

            return new string(array, 0, array.Length);
        }

        public static string Repeat(this char c, int numRepeats)
        {
            char[] arr = new char[numRepeats];
            for (int i = 0; i < numRepeats; i++)
                arr[i] = c;
            return new string(arr, 0, arr.Length);
        }

        public static char[] Repeat(this char[] chars, int numRepeats)
        {
            char[] arr = new char[chars.Length * numRepeats];
            var o = 0;
            for (int i=0; i < numRepeats; i++)
            {
                foreach (var c in chars)
                    arr[o++] = c;
            }

            return arr;
        }

        public static IEnumerable<string> SplitAndWrap(this string str, BbsSession session, OutputHandlingFlag flags)
        {
            if (string.IsNullOrWhiteSpace(str) || session == null)
            {
                yield return str;
                yield break;
            }

            int start = 0;
            int col = 0;
            int breakIndex = 0;
            bool inAnsiCode = false;
            bool trimStartOfNextLine = false;
            char enter = session.Io.NewLine?.FirstOrDefault() ?? (char)13;
            var newline = session.Io.NewLine;

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

                if (char.IsWhiteSpace(c) && true != newline?.Any(x => x == c))
                    breakIndex = i;

                // If CBM use Cols-1 because if we print a character on the last column then that will shift the cursor
                // down to the next line, even though we haven't reached a newline character.
                var maxCols = session.Cols;
                if (session.Io.EmulationType == TerminalEmulation.Cbm)
                    maxCols--;

                if (c == enter || col >= maxCols || i==str.Length-1)
                {
                    int end = breakIndex > start ? breakIndex : i;
                    if (i == str.Length - 1)
                        end = str.Length - 1;
                    else if (c == enter)
                        end = i;

                    int len = end - start;
                    string substring = str.Substring(start, len);                    

                    if (!string.IsNullOrWhiteSpace(substring))
                    {
                        substring = substring
                            .TrimEnd(' ')
                            .Replace("\r\0", "\r");

                        trimStartOfNextLine = false;

                        if (true != newline.Any(x => x == substring[substring.Length - 1])) // != 13 && substring[substring.Length - 1] != 10)
                        {
                            // adding a newline due to wrapping
                            substring += newline;
                            trimStartOfNextLine = true;
                        }
                        else if (substring[substring.Length - 1] == 13 && enter == 13) // don't do this for atascii
                        {
                            // replace just "enter" with proper newline (13 + 10, enter + linefeed)
                            substring = substring.Substring(0, substring.Length-1) + newline;
                        }
                        if (StartsWithNewLine(newline, substring))
                        {
                            // this line starts with a newline, take that off
                            substring = substring.Substring(newline.Length);                            
                        }

                        if (!string.IsNullOrWhiteSpace(substring))
                        {
                            if (trimStartOfNextLine || !flags.HasFlag(OutputHandlingFlag.DoNotTrimStart))
                                substring = substring.TrimStart(' ');
                            yield return substring.Replace(Constants.Spaceholder, ' ');
                        }
                    }
                    else 
                        yield return session.Io.NewLine;

                    start = end;
                    i = start;
                    col = 0;
                }
            }
        }

        private static bool StartsWithNewLine(string newline, string substring)
        {
            if (substring.Length <= newline.Length)
                return false;

            for (int i = 0; i < newline.Length; i++)
            {
                if (substring[i] != newline[i])
                    return false;
            }

            return true;
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

        public static string PadAndCenter(this string str, int totalLength)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;

            int spacesToAdd = totalLength - str.Length;
            if (spacesToAdd > 0)
            {
                var pad = ' '.Repeat(spacesToAdd / 2);
                str = $"{pad}{str}{pad}";
            }

            if (str.Length < totalLength)
                str = str.PadRight(totalLength);
            else if (str.Length > totalLength)
                str = str.Substring(totalLength);

            return str;
        }

        public static string JoinPathParts(params string[] parts)
        {
            if (true != parts?.Any())
                return string.Empty;

            var result = string.Join("/", parts.Select(p =>
            {
                p = p.Replace("\\", "/");
                if (p.StartsWith("/"))
                    p = p.Substring(1);
                if (p.EndsWith("/"))
                    p = p.Substring(0, p.Length - 1);
                return p;
            }));

            return result;
        }

        /// <summary>
        /// returns only the extension of the <paramref name="filename"/>
        /// </summary>
        public static string FileExtension(this string filename)
        {
            string result = string.Empty;

            if (!string.IsNullOrWhiteSpace(filename))
            {
                int pos = filename.LastIndexOf('.');
                if (pos > 0)
                    result = filename.Substring(pos + 1);
            }

            return result;
        }

        /// <summary>
        /// Returns the <paramref name="filename"/> without the extension
        /// </summary>
        public static string WithoutExtension(this string filename)
        {
            string result = filename;

            if (!string.IsNullOrWhiteSpace(filename))
            {
                int pos = filename.LastIndexOf('.');
                if (pos > 0)
                    result = filename.Substring(0, pos);
            }

            return result;
        }

        //public static string UniqueColor(this string str)
        //{
        //    if (string.IsNullOrWhiteSpace(str))
        //        return string.Empty;
        //    var hash = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(str));
        //    var sum = hash.Sum(b => b);
        //    sum %= 16;
        //    var color = (ConsoleColor)(sum % 16);
        //    if (color == ConsoleColor.Black)
        //        color = ConsoleColor.DarkGray;
        //    return Color(str, color);
        //}

        public static string Inverse(this string str) => $"{Constants.Inverser}{str}{Constants.Inverser}";

        public static string Color(this string str, ConsoleColor color)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;

            return UserIoExtensions.WrapInColor(str, color);
        }

        /// <summary>
        /// Returns the number of times the <paramref name="substring"/> occures within <paramref name="str"/>
        /// </summary>
        public static int Count(this string str, string substring)
        {
            if (string.IsNullOrWhiteSpace(str) || string.IsNullOrWhiteSpace(substring))
                return 0;

            int pos = -1;
            int count = 0;
            do
            {
                pos = str.IndexOf(substring, pos+1, StringComparison.CurrentCultureIgnoreCase);
                if (pos >= 0)
                    count++;
            } while (pos >= 0);
            return count;
        }

        public static string HtmlSafe(this string str)
        {
            return str
                ?.Replace("'", "&apos;")
                ?.Replace("<", "&lt;")
                ?.Replace(">", "&gt;");
        }

        /// <summary>
        /// Returns a string showing the number of days, hours, and minutes.  
        /// Only non-zero values are returned.  If <paramref name="timespan"/> is less than one minute then 
        /// returns the number of seconds.
        /// </summary>
        public static string Dhm(this TimeSpan timespan)
        {
            var builder = new StringBuilder();
            var days = timespan.Days;
            var hours = timespan.Hours;
            var minutes = timespan.Minutes;

            if (days > 0) builder.Append($"{days}d ");
            if (hours > 0) builder.Append($"{hours}h ");
            if (minutes > 0) builder.Append($"{minutes}m");

            if (builder.Length == 0)
                builder.Append($"{Math.Ceiling(timespan.TotalSeconds)}s");

            return builder.ToString();
        }

        public static byte[] ToByteArray(this string str) => (str ?? string.Empty).Select(c => (byte)c).ToArray();
    }
}
