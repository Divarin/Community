using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Extensions;
using miniBBS.Basic.Models;
using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace miniBBS.Basic.Executors
{
    public static class Evaluate
    {
        private static readonly DataTable _computer = new DataTable();
        private const int _maxEvaluationTimeSec = 10;

        private static readonly string[] _mathFunctions = new string[]
        { 
            "RND", "SIN", "COS", "TAN", "ATAN", "ASIN", "ACOS", "SQR", "VAL", "STR$", "INT", "POW", "MOD",
            "CHR$", "ASC", "LEFT$", "RIGHT$", "MID$", "REPLACE$", "INSTR", "LEN", "TAB", "ABS", "UC$", "LC$",
            "COUNT", "NL$", "REPEAT$", "ISWORD",
            "GETWORD", "GETWORDCONTAINS", "GETNEXTWORD", "GETNEXTWORDCONTAINS",
            "GETWORD$", "GETWORDCONTAINS$", "GETNEXTWORD$", "GETNEXTWORDCONTAINS$",
            "GUID$", "SECONDS", "LTRIM$", "RTRIM$", "TRIM$", "ROUND",
            "ISOPEN", "FILEPOSITION"
        };

        private static readonly char[] _logicalOperators = new char[]
        { '&', '|', '!' };
        private static readonly char[] _relationalOperators = new char[]
        { '=', '<', '>', '≤', '≥', '±' };
        private static readonly char[] _arithmeticOperators = new char[]
        { '+', '-', '*', '/', '%' };
        private static readonly char[] _charsInVariableNamesAfterFirst = new char[]
        { '$', '(', '\'', ',', '\t', ')', '_', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        public static string Execute(BbsSession session, string statement, Variables variables)        
        {
            if (string.IsNullOrWhiteSpace(statement))
                return statement;

            var pkg = new ExecutionPackage
            {
                OriginalStatement = statement,
                StringValues = new Dictionary<string, string>(),
                Timer = Stopwatch.StartNew(),
                Session = session,
            };

            statement = Execute(statement, variables, pkg);
            
            // re-insert string values in place of tokens
            foreach (var i in pkg.StringValues.Keys)
            {
                string key = $"¿[{i}]¿";
                string value = pkg.StringValues[i];
                statement = statement.Replace(key, value);
            }

            statement = statement.Replace(Constants.Basic.QuoteSubstitute, Constants.Basic.Quote);
            return statement;
        }

        private static string Execute(string statement, Variables variables, ExecutionPackage pkg)
        {
            if (pkg.Timer.Elapsed.TotalSeconds > _maxEvaluationTimeSec)
            {
                throw new RuntimeException($"ERROR Evaluating '{statement}' component of expression '{pkg.OriginalStatement}', maximum evaluation time exceeded!");
            }

            InnerExpression inner = GetInnerExpression(statement);
            if (inner != null)
            {
                string innerValue = Execute(inner.Expression, variables, pkg);
                string s =
                    statement.Substring(0, inner.Start) +
                    innerValue +
                    statement.Substring(inner.End, statement.Length - inner.End);
                if (s != statement)
                    return Execute(s, variables, pkg);
            }

            // tokenize string constants
            statement = TokenizeStrings(statement, pkg.StringValues);
            // replace variables with their constant values (including strings)
            statement = Substitute(variables, statement, pkg);
            // re-tokenize to replace the new string constants with tokens
            statement = TokenizeStrings(statement, pkg.StringValues);
            // tokenize relational and logical operators with single character substitutes (makes splitting easier)
            statement = statement
                .Replace("<=", "≤")
                .Replace("=<", "≤")
                .Replace(">=", "≥")
                .Replace("=>", "≥")
                .Replace("<>", "±")
                .Replace("><", "±")
                .Replace(" AND ", "&").Replace(" and ", "&")
                .Replace(" OR ", "|").Replace(" or ", "|")
                .Replace("NOT ", "!").Replace("not ", "!");

            // with all strings tokenized, remove all spaces
            statement = statement
                .Replace(" ", "");
            //.Replace("\t", "");

            // concatenate strings
            statement = statement
                .Replace("]¿+¿[", "]¿¿[");

            // evaluate string tokens related to other string tokens
            statement = ConvertStringRelationsToNumericRelations(statement);

            // do all logical and arithmatic computations
            statement = DoComputations(statement);

            // replace "true" with 1 and "false" with 0
            statement = statement.Replace("True", "1");
            statement = statement.Replace("False", "0");

            // replace commas with tabs and semicolons with nothing
            statement = statement
                .Replace(',', '\t')
                .Replace(";", "");

            return statement;
        }

        private static InnerExpression GetInnerExpression(string statement)
        {
            if (true != statement?.Any(x => x == '('))
                return null;

            var innerExpression = new InnerExpression();
            StringBuilder builder = new StringBuilder();

            bool str, startFound;
            str = startFound = false;
            int openParens = 0;

            for (int i = 0; i < statement.Length; i++)
            {
                char c = statement[i];
                if (c == '"')
                    str = !str;
                else if (c == '(' && !str && !startFound)
                {
                    startFound = true;
                    innerExpression.Start = i + 1;
                    continue;
                }
                else if (c == ')' && !str && startFound && openParens == 0)
                {
                    innerExpression.End = i;
                    innerExpression.Expression = builder.ToString();
                    return innerExpression;
                }
                else if (c == '(' && !str && startFound)
                    openParens++;
                else if (c == ')' && !str)
                    openParens--;

                if (startFound)
                    builder.Append(c);
            }

            return null;
        }

        private static string DoComputations(string statement)
        {
            char[] ops = _relationalOperators
                .Union(_arithmeticOperators)
                .Union(_logicalOperators)
                .ToArray();

            string[] parts = statement.Split(new char[] { ';', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts?.Length < 1)
                parts = new string[] { statement };

            foreach (var op in ops)
            {
                var partsWithOp = parts.Where(x => x.Contains(op)).ToList();
                if (true == partsWithOp?.Any())
                {
                    for (int i=0; i < partsWithOp.Count; i++)
                    {
                        string part = partsWithOp[i];
                        // convert back correct operator strings
                        // this is only used temporarily in this method
                        string convertedPart = part
                            .Replace("≤", "<=")
                            .Replace("≥", ">=")
                            .Replace("±", "<>")
                            .Replace("!", " NOT ")
                            .Replace("&", " AND ")
                            .Replace("|", " OR ");

                        try
                        {
                            string result = _computer.Compute(convertedPart, string.Empty)?.ToString();
                            statement = statement.Replace(part, result);
                        } catch (Exception ex)
                        {
                            throw new RuntimeException(ex.Message);
                        }
                    }
                }
            }

            return statement;
        }

        private static string ConvertStringRelationsToNumericRelations(string statement)
        {
            char[] chars = statement.ToArray();

            for (int i=0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (_relationalOperators.Contains(c))
                {
                    // remove ¿[]¿ from left
                    int replaced = 0;
                    for (int j=i; j>=0 && replaced < 4; j--)
                    {
                        char cj = chars[j];
                        if (cj == '¿' || cj == '[' || cj == ']')
                        {
                            replaced++;
                            chars[j] = ' ';
                        }
                    }

                    // remove ¿[]¿ from right
                    replaced = 0;
                    for (int j = i; j < chars.Length && replaced < 4; j++)
                    {
                        char cj = chars[j];
                        if (cj == '¿' || cj == '[' || cj == ']')
                        {
                            replaced++;
                            chars[j] = ' ';
                        }
                    }
                }
            }

            return new string(chars, 0, chars.Length);
        }

        private static string TokenizeStrings(string statement, IDictionary<string, string> tokenDict)
        {
            bool str = false;
            StringBuilder strBuilder = new StringBuilder();

            for (int i = 0; i < statement.Length; i++)
            {
                char c = statement[i];
                if (c == '"')
                {
                    if (str)
                    {
                        // now leaving string
                        // take string value add to list
                        string strValue = strBuilder.ToString();
                        var token = CreateStringToken(strValue);
                        if (!tokenDict.ContainsKey(token))
                            tokenDict[token] = strValue;

                        strBuilder.Clear();
                        // replace occurances of this string with its token
                        statement = statement.Replace("\"" + strValue + "\"", $"¿[{token}]¿");
                        i = -1; // restart loop
                    }
                    str = !str;
                }
                else if (str)
                {
                    strBuilder.Append(c);
                }

            }

            return statement;
        }

        private static string CreateStringToken(string strValue)
        {
            if (strValue == null)
                return "0";

            var bytes = strValue.Select(x => (byte)x).ToArray();
            var result = string.Join("", bytes.Select(b => $"{b:000}"));

            return result;
        }

        private static string Substitute(Variables variables, string exp, ExecutionPackage pkg)
        {
            exp = SubstituteMaths(variables, exp, pkg);

            Dictionary<string, string> scopedVariables = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            
            foreach (var v in variables.PeekAllScoped()
                ?.SelectMany(x => x.LocalVariables)
                ?.OrderByDescending(x => x.Key.Length))
            {
                scopedVariables[v.Key] = v.Value;
            }

            StringBuilder builder = new StringBuilder();
            List<char> varBuilder = new List<char>();
            bool quote = false;

            Action<string> HandlePossibleVariable = vrb =>
            {
                // encountered end of possible variable name
                if (scopedVariables.ContainsKey(vrb))
                    builder.Append(scopedVariables[vrb]);
                else if (variables[vrb] != null)
                    builder.Append(variables[vrb]);
                else
                    builder.Append(vrb);
                varBuilder.Clear();
            };

            for (int i=0; i < exp.Length; i++)
            {
                char c = exp[i];
                if (c == '"')
                {
                    quote = !quote;
                    builder.Append(c);
                }
                else
                {
                    if (!quote)
                    {
                        bool couldBePartOfAVariableName =
                            char.IsLetter(c) ||
                            (c == '_' && varBuilder.Count == 0) ||
                            (varBuilder.Count > 0 && _charsInVariableNamesAfterFirst.Contains(c));

                        couldBePartOfAVariableName &= c != ')' || varBuilder.Any(_c => _c == '(');

                        if (couldBePartOfAVariableName)
                        {
                            varBuilder.Add(c);
                            if ( i == exp.Length-1 ||
                                 c == ')' ||
                                (c == '$' && exp[i+1] != '(') && exp[i+1] != ')')
                                HandlePossibleVariable(new string(varBuilder.ToArray()));
                        }
                        else if (varBuilder.Count > 0)
                        {
                            // encounterd end of possible variable name
                            HandlePossibleVariable(new string(varBuilder.ToArray()));
                            builder.Append(c);
                        }
                        else
                            builder.Append(c);
                    }
                    else
                        builder.Append(c); // append char found in string
                }
            }
            return builder.ToString();
        }

        private static string SubstituteMaths(Variables variables, string line, ExecutionPackage pkg)
        {
            var functions = _mathFunctions.Union(variables.Functions.Select(x => x.Name)).ToArray();

            foreach (var f in functions)
            {
                string funcName = $"{f}(";
                int pos = -1;
                do
                {
                    pos = line.IndexOf(funcName, StringComparison.CurrentCultureIgnoreCase);
                    if (pos < 0)
                        break;

                    int end = line.IndexOfClosingParens(pos);// line.IndexOf(')', pos);

                    if (end < pos)
                        break;

                    funcName = line.Substring(pos, funcName.Length); // match case with what user typed in
                    pos += funcName.Length;
                    string arg = line.Substring(pos, end - pos);
                    //if (arg.StartsWith("("))
                    //    arg = arg.Substring(1);
                    var value = Execute(arg, variables, pkg);
                    switch (f.ToLower())
                    {
                        case "sqr": value = Sqr.Execute(value).ToString(); break;
                        case "rnd": value = Rnd.Execute().ToString(); break;
                        case "sin": value = Math.Sin(double.Parse(value)).ToString(); break;
                        case "cos": value = Math.Cos(double.Parse(value)).ToString(); break;
                        case "tan": value = Math.Tan(double.Parse(value)).ToString(); break;
                        case "atan": value = Math.Atan(double.Parse(value)).ToString(); break;
                        case "asin": value = Math.Asin(double.Parse(value)).ToString(); break;
                        case "acos": value = Math.Acos(double.Parse(value)).ToString(); break;
                        case "pow":
                            {
                                string[] parts = value.Split(new char[] { ',', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                var a = Execute(parts[0], variables, pkg);
                                var b = Execute(parts[1], variables, pkg);
                                value = Math.Pow(double.Parse(a), double.Parse(b)).ToString();
                            }
                            break;
                        case "mod":
                            {
                                string[] parts = value.Split(new char[] { ',', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                var a = Execute(parts[0], variables, pkg);
                                var b = Execute(parts[1], variables, pkg);
                                if (double.TryParse(a, out double dA) && int.TryParse(b, out int iB))
                                    value = ((int)(dA % iB)).ToString();
                            }
                            break;
                        case "int": value = Int.Execute(value).ToString(); break;
                        case "chr$":
                            {
                                if (string.IsNullOrWhiteSpace(value) || !byte.TryParse(value, out byte b))
                                    throw new RuntimeException($"Unable to parse '{value}' as a numeric value.");
                                var c = (char)b;
                                //var c = (char)byte.Parse(value);
                                if (c == Constants.Basic.Quote) c = Constants.Basic.QuoteSubstitute;
                                value = $"{Constants.Basic.Quote}{c}{Constants.Basic.Quote}";
                            }
                            break;
                        case "str$": value = $"{Constants.Basic.Quote}{value}{Constants.Basic.Quote}"; break;
                        case "guid$":
                            {
                                var parts = value.Split('\t')?.Select(x => x.Trim())?.ToArray();
                                var guid = Guid.NewGuid().ToString();
                                if (parts?.Length == 1 && int.TryParse(parts[0], out int v) && v > 0 && v <= 32)
                                    guid = new string(guid.Replace("-", "").Take(v).ToArray());
                                value = $"{Constants.Basic.Quote}{guid}{Constants.Basic.Quote}";
                            }
                            break;
                        case "seconds":
                            {
                                var parts = value.Split('\t')?.Select(x => x.Trim())?.ToArray();
                                if (parts.Length == 2 && 
                                    DateTime.TryParse(parts[0].Detokenize(pkg.StringValues), out var dt1) && 
                                    DateTime.TryParse(parts[1].Detokenize(pkg.StringValues), out var dt2))
                                    value = (dt2 - dt1).TotalSeconds.ToString();
                            }
                            break;
                        case "left$":
                            {
                                var parts = value.Split('\t')?.Select(x => x.Trim())?.ToArray();
                                if (parts?.Length != 2)
                                    throw new RuntimeException("invalid input for left$() function");

                                if (!int.TryParse(parts[1], out int count) || count < 1)
                                    throw new RuntimeException("invalid input for left$() function");

                                parts[0] = parts[0].Detokenize(pkg.StringValues);

                                if (count < 0)
                                    throw new RuntimeException("invalid input for left$() function");

                                if (count >= parts[0].Length)
                                    value = $"{Constants.Basic.Quote}{parts[0]}{Constants.Basic.Quote}";
                                else
                                    value = $"{Constants.Basic.Quote}{parts[0].Substring(0, count)}{Constants.Basic.Quote}";
                            }
                            break;
                        case "right$":
                            {
                                var parts = value.Split('\t')?.Select(x => x.Trim())?.ToArray();
                                if (parts?.Length != 2)
                                    throw new RuntimeException("invalid input for right$() function");

                                int count;
                                if (!int.TryParse(parts[1], out count))
                                    throw new RuntimeException("invalid input for right$() function");

                                parts[0] = parts[0].Detokenize(pkg.StringValues);
                                if (count < 0)
                                    throw new RuntimeException("invalid input for right$() function");

                                if (count >= parts[0].Length)
                                    value = $"{Constants.Basic.Quote}{parts[0]}{Constants.Basic.Quote}";
                                else
                                    value = $"{Constants.Basic.Quote}{parts[0].Substring(parts[0].Length - count)}{Constants.Basic.Quote}";
                            }
                            break;
                        case "mid$":
                            {
                                var parts = value.Split('\t')?.Select(x => x.Trim())?.ToArray();
                                if (parts?.Length != 3)
                                    throw new RuntimeException($"invalid input for mid$() function, expected 3 parameters and got {parts?.Length}");

                                int start;
                                parts[1] = Execute(parts[1], variables, pkg);

                                if (!int.TryParse(parts[1], out start))
                                    throw new RuntimeException($"invalid input for mid$() function, expected start index to be a number but {parts[1]} isn't");
                                start--;

                                int count;
                                parts[2] = Execute(parts[2], variables, pkg);

                                if (!int.TryParse(parts[2], out count))
                                    throw new RuntimeException($"invalid input for mid$() function, expected count to be a number but {parts[2]} isn't");

                                if (start < 0 || count < 0)
                                    throw new RuntimeException($"invalid input for mid$() function, start={start} and count={count}");

                                parts[0] = parts[0].Detokenize(pkg.StringValues);

                                if (start >= parts[0].Length)
                                {
                                    value = $"{Constants.Basic.Quote}{parts[0]}{Constants.Basic.Quote}";
                                    break;
                                }

                                if (start + count > parts[0].Length)
                                    count = parts[0].Length - start;

                                value = $"{Constants.Basic.Quote}{parts[0].Substring(start, count)}{Constants.Basic.Quote}";
                            }
                            break;
                        case "replace$":
                            {
                                string[] parts = value.Split(new char[] { ',', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length != 3)
                                {
                                    throw new RuntimeException("Invalid number of parameters for replace$(), must pass three strings.");
                                }
                                var source = Execute(parts[0], variables, pkg);
                                var search = Execute(parts[1], variables, pkg);
                                var replacement = Execute(parts[2], variables, pkg);
                                source = source.Detokenize(pkg.StringValues);
                                search = search.Detokenize(pkg.StringValues);
                                replacement = replacement.Detokenize(pkg.StringValues);
                                value = "\"" + source.Replace(search, replacement) + "\"";
                            }
                            break;
                        case "count":
                            {
                                string[] parts = value.Split(new char[] { ',', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length != 2)
                                {
                                    throw new RuntimeException("Invalid number of parameters for count(), must pass two strings.");
                                }
                                var haystack = Execute(parts[0], variables, pkg).Detokenize(pkg.StringValues);
                                var needle = Execute(parts[1], variables, pkg).Detokenize(pkg.StringValues);                                
                                var count = haystack.Count(needle);
                                value = $"{count}";
                            }
                            break;
                        case "nl$":
                            {
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    string strN = Execute(value, variables, pkg).Detokenize(pkg.StringValues);
                                    if (int.TryParse(strN, out int n) && n > 1 && n < 100)
                                        value = Environment.NewLine.Repeat(n);
                                    else
                                        value = Environment.NewLine;
                                }
                                else
                                    value = Environment.NewLine;
                            }
                            break;
                        case "uc$":
                            {
                                value = value.Detokenize(pkg.StringValues);
                                value = Constants.Basic.Quote + value.ToUpper() + Constants.Basic.Quote;
                            }
                            break;
                        case "lc$":
                            {
                                value = value.Detokenize(pkg.StringValues);
                                value = Constants.Basic.Quote + value.ToLower() + Constants.Basic.Quote;
                            }
                            break;
                        case "instr":
                            {
                                var _parts = value.SplitWithStringsIntact(new char[] { ',', '\t' });
                                if (_parts?.Length != 2)
                                    throw new RuntimeException("Incorrect number of arguments for instr function");
                                string _haystack = _parts[0];
                                string _needle = _parts[1];
                                _haystack = _haystack.Detokenize(pkg.StringValues);
                                _needle = _needle.Detokenize(pkg.StringValues);
                                int _pos = Instr.Execute(_haystack, _needle);
                                value = _pos.ToString();
                            }
                            break;
                        case "len":
                            {
                                int _len = Len.Execute(value.Detokenize(pkg.StringValues));
                                value = _len.ToString();
                            }
                            break;
                        case "val":
                            {
                                value = value.Detokenize(pkg.StringValues);
                                if (double.TryParse(value, out double d))
                                    value = d.ToString();
                                else
                                    value = "0";
                            }
                            break;
                        case "asc":
                            {
                                value = value.Detokenize(pkg.StringValues);
                                if (value.Length > 0)
                                {
                                    value = $"{(int)value[0]}";
                                }
                            }
                            break;
                        case "tab":
                            {
                                int count = 1;
                                if (!string.IsNullOrWhiteSpace(value))
                                    int.TryParse(value, out count);
                                value = '"' + " ".Repeat(count) + '"';
                            }
                            break;
                        case "repeat$":
                            {
                                string[] parts = value.Split(new char[] { ',', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length != 2)
                                {
                                    throw new RuntimeException("Invalid number of parameters for repeat$(), must pass one string and one number.");
                                }
                                var str = Execute(parts[0], variables, pkg).Detokenize(pkg.StringValues);
                                var strCount = Execute(parts[1], variables, pkg).Detokenize(pkg.StringValues);
                                if (int.TryParse(strCount, out var count))
                                    value = '"' + str.Repeat(count) + '"';
                            }
                            break;
                        case "abs":
                            {
                                if (string.IsNullOrWhiteSpace(value) || !double.TryParse(value, out double d))
                                    throw new RuntimeException("type mismatch");
                                value = Math.Abs(d).ToString();
                            }
                            break;
                        case "trim$":
                            {
                                value = value.Detokenize(pkg.StringValues);
                                value = value.Trim();
                                value = $"{Constants.Basic.Quote}{value}{Constants.Basic.Quote}";
                            }
                            break;
                        case "ltrim$":
                            {
                                value = value.Detokenize(pkg.StringValues);
                                value = value.TrimStart();
                                value = $"{Constants.Basic.Quote}{value}{Constants.Basic.Quote}";
                            }
                            break;
                        case "rtrim$":
                            {
                                value = value.Detokenize(pkg.StringValues);
                                value = value.TrimEnd();
                                value = $"{Constants.Basic.Quote}{value}{Constants.Basic.Quote}";
                            }
                            break;
                        case "round":
                            {
                                string[] parts = value.Split(new char[] { ',', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                var a = Execute(parts[0], variables, pkg);
                                var b = Execute(parts[1], variables, pkg);
                                value = Math.Round(double.Parse(a), int.Parse(b)).ToString();
                            }
                            break;
                        case "isword":
                            value = Words.IsWord(value.Detokenize(pkg.StringValues)) ? "True" : "False";
                            break;
                        case "getword$":
                        case "getword":
                            {
                                if (string.IsNullOrWhiteSpace(value))
                                    value = Words.GetWord();
                                else
                                {
                                    var args = value.Split('\t');
                                    if (args.Length == 1)
                                    {
                                        if (int.TryParse(args[0], out int _length))
                                            value = '"' + Words.GetWord(_length) + '"';
                                        else
                                            value = '"' + Words.GetWord(args[0].Detokenize(pkg.StringValues)) + '"';
                                    }
                                    else if (args.Length == 2 && int.TryParse(args[0], out int _minlength) && int.TryParse(args[1], out int _maxlength))
                                        value = '"' + Words.GetWord(_minlength, _maxlength) + '"';
                                }
                            }
                            break;
                        case "getwordcontains$":
                        case "getwordcontains":
                            value = '"' + Words.GetWordContains(value.Detokenize(pkg.StringValues)) + '"';
                            break;
                        case "getnextword$":
                        case "getnextword":
                            {
                                if (string.IsNullOrWhiteSpace(value))
                                    value = Words.GetWord(unique: true);
                                else
                                {
                                    var args = value.Split('\t');
                                    if (args.Length == 1)
                                    {
                                        if (int.TryParse(args[0], out int _length))
                                            value = '"' + Words.GetWord(_length, unique: true) + '"';
                                        else
                                            value = '"' + Words.GetWord(args[0].Detokenize(pkg.StringValues), unique: true) + '"';
                                    }
                                    else if (args.Length == 2 && int.TryParse(args[0], out int _minlength) && int.TryParse(args[1], out int _maxlength))
                                        value = '"' + Words.GetWord(_minlength, _maxlength, unique: true) + '"';
                                }
                            }
                            break;
                        case "getnextwordcontains$":
                        case "getnextwordcontains":
                            value = '"' + Words.GetWordContains(value.Detokenize(pkg.StringValues), unique: true) + '"';
                            break;
                        case "isopen":
                            {
                                value = Execute(value, variables, pkg);
                                if (pkg.Session != null &&
                                    int.TryParse(value, out var fileNum) &
                                    Files.IsOpen(pkg.Session, fileNum))
                                    value = "1";
                                else
                                    value = "0";
                            }
                            break;
                        case "fileposition":
                            {
                                value = Execute(value, variables, pkg);
                                if (pkg.Session != null &&
                                    int.TryParse(value, out var fileNum))
                                    value = $"{Files.FilePosition(pkg.Session, fileNum)}";
                                else
                                    value = "-1";
                                break;
                            }
                        case "defined":
                            value = value.Detokenize(pkg.StringValues);
                            if (variables.IsDefined(value, StringComparer.OrdinalIgnoreCase))
                                value = "1";
                            else
                                value = "0";
                            break;
                        default:
                            if (f.StartsWith("fn", StringComparison.CurrentCultureIgnoreCase))
                            {
                                // execute function call: FN(X,Y,Z)
                                Function function = variables.Functions?.FirstOrDefault(x => x.Name.Equals(f, StringComparison.CurrentCultureIgnoreCase));
                                if (function != null)
                                {
                                    double d = function.Execute(pkg.Session, value, variables);
                                    value = d.ToString();
                                }
                            }
                            break;
                    }
                    line = line.Replace($"{funcName}{arg})", value);
                } while (pos >= 0);
            }

            return line;
        }
     
        private class InnerExpression
        {
            public string Expression { get; set; }
            public int Start { get; set; }
            public int End { get; set; }

            public override string ToString()
            {
                return Expression;
            }
        }

        private class ExecutionPackage
        {
            /// <summary>
            /// [Token] = string value
            /// </summary>
            public IDictionary<string, string> StringValues { get; set; }
            public Stopwatch Timer { get; set; }
            public string OriginalStatement { get; set; }
            public BbsSession Session { get; internal set; }
        }
    }
}
