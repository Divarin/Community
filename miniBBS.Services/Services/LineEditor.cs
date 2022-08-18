using miniBBS.Core;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace miniBBS.Services.Services
{
    public class LineEditor : ITextEditor
    {
        private BbsSession _session;
        private string _savedText = null;

        public void EditText(BbsSession session, string existingText = null)
        {
            bool wasDnd = session.DoNotDisturb;
            session.DoNotDisturb = true;
            List<string> lines = null;

            try
            {
                lines = new List<string>();
                if (!string.IsNullOrWhiteSpace(existingText))
                    lines.AddRange(existingText
                        .Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
                        .Select(l => l.TrimEnd()));

                _savedText = string.Join(Environment.NewLine, lines);
                _session = session;

                var cmdResult = CommandResult.None;

                session.Io.OutputLine($"Mutiny Community Line Editor.  Type '/?' on a blank line for help.");
                session.Io.OutputLine($" {'-'.Repeat(session.Cols - 3)} ");

                if (lines.Count > 0)
                {
                    session.Io.OutputLine($"{lines.Count} line(s) loaded, use '/l' to list.");
                }

                while (!cmdResult.HasFlag(CommandResult.ExitEditor))
                {
                    var line = session.Io.InputLine();
                    if (String.IsNullOrWhiteSpace(line))
                        lines.Add(string.Empty);
                    else if (line.StartsWith("/"))
                    {
                        cmdResult = ProcessCommand(lines, line.Substring(1).Split(' ').ToArray());
                        if (cmdResult.HasFlag(CommandResult.Saved))
                            _savedText = String.Join(Environment.NewLine, lines);
                        if (cmdResult.HasFlag(CommandResult.RevertToOriginal))
                        {
                            if ('Y' == _session.Io.Ask("Are you sure you want to undo changes since your last save?: "))
                            {
                                lines.Clear();
                                if (!string.IsNullOrWhiteSpace(_savedText))
                                    lines.AddRange(_savedText
                                        .Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
                                        .Select(l => l.TrimEnd()));
                            }
                        }
                    }
                    else
                    {
                        lines.Add(line);
                    }
                };
            } 
            catch (Exception)
            {
                // something went wrong, possibly a connection failure, auto-save before throwing exception
                if (OnSave != null && HasUnsavedChanges(lines))
                {
                    var body = Compile(lines);
                    OnSave(body);
                }
                throw;
            }
            finally
            {
                session.DoNotDisturb = wasDnd;
            }
        }

        private bool HasUnsavedChanges(List<string> lines)
        {
            if (lines == null)
                return false;

            string text = string.Join(Environment.NewLine, lines);
            return text != _savedText;
        }

        /// <summary>
        /// Func that takes the text body, saves it (somehow), and returns a status message such as 
        /// "saved as 'myfile.txt'" or something.
        /// </summary>
        public Func<string, string> OnSave { get; set; }

        private CommandResult ProcessCommand(List<string> lines, string[] args)
        {
            if (args?.Length < 1)
                return CommandResult.None;
            
            switch (args[0].ToLower())
            {
                case "save":
                case "s":
                case "sq":
                    // save
                    if (OnSave != null)
                    {
                        var body = Compile(lines);
                        var status = OnSave(body);
                        Notify(status);
                        var result = CommandResult.Saved;
                        if ("sq".Equals(args[0], StringComparison.CurrentCultureIgnoreCase))
                            result |= CommandResult.ExitEditor;
                        return result;
                    }
                    break;
                case "abort":
                case "abt":
                case "a":
                    // abort
                    return CommandResult.ExitEditor | CommandResult.RevertToOriginal;
                case "quit":
                case "exit":
                case "x":
                case "q":
                    // quit
                    {
                        var result = CommandResult.ExitEditor;
                        if (HasUnsavedChanges(lines) && OnSave != null)
                        {
                            Ask("Save changes first? (Y)es, (N)o, (A)bort (don't quit)", new Dictionary<char, Action>
                            {
                                {'n', () =>
                                { 
                                    result |= CommandResult.RevertToOriginal;
                                    Notify("Undoing changes");
                                } },
                                {'a', () => 
                                {
                                    result = CommandResult.None;
                                    Notify("Continue editing");
                                } },
                                {'y', () =>
                                {
                                    var body = Compile(lines);
                                    var status = OnSave(body);
                                    Notify(status);
                                } }
                            });
                        }
                        return result;
                    }
                case "list":
                case "l":
                    // list (with or without line numbers)
                    {
                        var range = args.Length >= 2 ? args[1] : null;
                        Ask("With line numbers? (Y)es, (N)o, (A)bort", new Dictionary<char, Action>
                        {
                            {'n', () => List(lines, false, range) },
                            {'y', () => List(lines, true, range) }
                        });
                    }
                    break;
                case "insert":
                case "ins":
                case "i":
                    // insert blank line(s)
                    {
                        if (args.Length < 2)
                            Notify("Insert after what line? (try '/i 5' to insert after line 5)");
                        else
                            InsertLines(lines, args.Skip(1)?.ToArray());
                    }
                    break;
                case "delete":
                case "del":
                case "d":
                    // delete line(s)
                    {
                        var range = args.Length >= 2 ? args[1] : null;
                        Delete(lines, range);
                    }
                    break;
                case "edit":
                case "ed":
                case "e":
                    // edit a line
                    Edit(lines, args.Skip(1)?.ToArray());
                    break;
                case "find":
                case "f":
                    // find
                    Find(lines, string.Join(" ", args.Skip(1)));
                    break;
                case "replace":
                case "r":
                    // replace
                    Replace(lines, args.Skip(1)?.ToArray());
                    break;
                case "import":
                    Import(lines, args.Skip(1)?.FirstOrDefault());
                    break;
                case "help":
                case "h":
                case "?":
                    // help
                    Help();
                    break;
            }

            return CommandResult.None;
        }

        private void Import(List<string> lines, string filename)
        {
            var dir = $"{Constants.UploadDirectory}{_session.User.Name.MaxLength(8, false)}";

            if (!Directory.Exists(dir))
            {
                _session.Io.OutputLine("Sorry you don't seem to have a dedicated upload directory on Mutiny BBS.  Please ask the sysop to create one for you.");
                return;
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                _session.Io.OutputLine($"Files found in your upload directory: {Environment.NewLine}{string.Join(", ", GetFiles(dir))}");
                return;
            }    

            if (!Directory.Exists(dir) ||
                filename.Any(c => c == '/' || c == '\\'))
            {
                _session.Io.OutputLine("Invalid import filename.");
                return;
            }

            filename = $"{Constants.UploadDirectory}{_session.User.Name.MaxLength(8, false)}\\{filename}";
            if (!File.Exists(filename))
            {
                _session.Io.OutputLine("Invalid import filename.");
                return;
            }

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(fs))
            {
                var data = reader.ReadToEnd();
                lines.AddRange(data.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
                        .Select(l => l.TrimEnd()));

                _session.Io.OutputLine("File contents imported, use '/l' to list.");
            }
        }

        private IEnumerable<string> GetFiles(string dir)
        {
            return new DirectoryInfo(dir)
                .GetFiles()
                .Select(f => f.Name);
        }

        private void Help()
        {
            using (_session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Cyan))
            {
                _session.Io.OutputLine(String.Join(Environment.NewLine, new[]
                {
                    "/save, /s : Save file (does not exit, use '/q' to quit)",
                    "/sq : Saves and then exits",
                    "/abort, /abt, /a : Abort (undoes all changes since your last save)",
                    "/quit, /exit, /q, /x : Exit the editor, if you have unsaved changes you're asked if you also want to save",
                    "/list, /l [range] : Lists the file's contents.  You will be asked if you want to show line numbers.  Optional range can be a line number or a range of lines (such as '/list 5' or '/list 5-10' or '/list 5-' or '/list -5'",
                    "/insert, /ins, /i (after) [count] : Inserts one or more blank lines after the specified line number.  Can use '/insert 0' to insert at the beginning",
                    "/delete, /del, /d [range] : Deletes one or more lines, if the optional [range] is not given then the last line is deleted.  You will be asked to confirm the delete",
                    "/edit, /ed, /e [line num] [search & replace]: Edits a line.  If a line number is given then edits that line otherwise edits the last line.",
                    $"{Constants.Spaceholder}Examples:",
                    $"{Constants.Spaceholder}/edit 50 - lets you re-type line 50",
                    $"{Constants.Spaceholder}/edit - lets you re-type the last line",
                    $"{Constants.Spaceholder}/edit 50 foo bar - replaces instances of 'foo' with 'bar' on line 50 (case sensitive)",
                    $"{Constants.Spaceholder}/edit foo bar - replaces instances of 'foo' with 'bar' on the last line (case sensitive)",
                    "/find, /f (search) : Lists all lines containing the (search)",
                    "/replace, /r (search) (replace) : Replaces all occurances of (search) with (replace) throughout the document (case sensitive).  Asks you for confirmation.",
                    "/import, /imp (filename) : Loads in the contents of a file uploaded to Mutiny BBS if it was uploaded to your text files upload area",
                    "/help, /h, /? : This menu"
                }));
            }
        }

        private void Replace(List<string> lines, string[] args)
        {
            string search, replace;
            search = replace = null;
            if (args?.Length == 1)
            {
                var arr = args[0]
                    .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToArray();
                if (arr.Length == 2)
                {
                    search = arr[0];
                    replace = arr[1];
                }
            }
            else if (args?.Length == 2)
            {
                search = args[0].Trim();
                replace = args[1].Trim();
            }
            else
            {
                Notify(
                    $"Please use '/r (old) (new)' where (old) is the word to find and (new) is what to replace it with.{Environment.NewLine}" +
                    "You can also use '/r (old)/(new)' (separated by a slash) if either (old) or (new) contains spaces.");
                return;
            }

            var builder = new StringBuilder();
            Dictionary<int, string> newLines = new Dictionary<int, string>();
            for (int i=0; i < lines.Count; i++)
            {
                string line = lines[i];
                string newLine = line.Replace(args[0], args[1]);
                if (newLine != line)
                {
                    newLines[i] = newLine;
                    builder.AppendLine($"{i + 1} : {line.Replace(args[0], $"{Constants.InlineColorizer}{(int)ConsoleColor.Red}{Constants.InlineColorizer}{args[0]}{Constants.InlineColorizer}{-1}{Constants.InlineColorizer}")}");
                    builder.AppendLine($"{i + 1} : {newLine.Replace(args[1], $"{Constants.InlineColorizer}{(int)ConsoleColor.Red}{Constants.InlineColorizer}{args[1]}{Constants.InlineColorizer}{-1}{Constants.InlineColorizer}")}");
                }
            }
            if (builder.Length > 0)
            {
                _session.Io.OutputLine(builder.ToString());
                Ask("Update these lines?", new Dictionary<char, Action>
                {
                    {'y', () =>
                    {
                        foreach (var l in newLines)
                        {
                            lines[l.Key] = l.Value;
                        }
                        Notify("Done");
                    } }
                });
            }
        }

        private void Find(List<string> lines, string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                Notify("Specify what you want to find '/find hello world'");
                return;
            }

            search = search.ToLower();
            var builder = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].ToLower().Contains(search))
                {
                    builder.AppendLine($"{i + 1} : {lines[i].Replace(search, $"{Constants.InlineColorizer}{(int)ConsoleColor.Red}{Constants.InlineColorizer}{search}{Constants.InlineColorizer}{-1}{Constants.InlineColorizer}")}");
                }
            }

            using (_session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                _session.Io.OutputLine(builder.ToString());
            }
        }

        private void Edit(List<string> lines, string[] args)
        {
            int lineNum = lines.Count - 1;
            if (true == args?.Length >= 1 && int.TryParse(args[0], out int ln) && ln > 0 && ln <= lines.Count)
                lineNum = ln-1;

            string search, replace;
            search = replace = null;

            if (args?.Length == 2)
            {
                search = args[0];
                replace = args[1];
            }
            else if (args?.Length == 3)
            {
                search = args[1];
                replace = args[2];
            }

            using (_session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                string line = lines[lineNum];
                _session.Io.OutputLine("Before:");
                _session.Io.SetForeground(ConsoleColor.White);
                _session.Io.OutputLine(line);
                _session.Io.SetForeground(ConsoleColor.Magenta);
                if (!string.IsNullOrWhiteSpace(search) && !string.IsNullOrWhiteSpace(replace))
                {
                    line = line.Replace(search, replace);
                }
                else
                {
                    var option = _session.Io.Ask($"P)repend to beginning{Environment.NewLine}A)ppend to end{Environment.NewLine}S)earch & replace{Environment.NewLine}R)etype new line{Environment.NewLine}Q)uit{Environment.NewLine}How to edit?: ");
                    line = Edit(line, option);
                }
                _session.Io.OutputLine("After:");
                _session.Io.SetForeground(ConsoleColor.White);
                _session.Io.OutputLine(line);
                Ask("Make this edit?", new Dictionary<char, Action>
                {
                    {'y', () =>
                    {
                        lines[lineNum] = line;
                        _session.Io.OutputLine("Line edited");
                    } }
                });
            }
        }

        private string Edit(string line, char option)
        {
            switch (option)
            {
                case 'P':
                    // prepend
                    {
                        _session.Io.Output("Text to prepend: ");
                        var append = _session.Io.InputLine();
                        _session.Io.OutputLine();
                        if (!string.IsNullOrWhiteSpace(append))
                            line = append + line;
                    }
                    break;
                case 'A':
                    // append
                    {
                        _session.Io.Output("Text to append: ");
                        var append = _session.Io.InputLine();
                        _session.Io.OutputLine();
                        if (!string.IsNullOrWhiteSpace(append))
                            line += append;
                    }
                    break;
                case 'S':
                    // search & replace
                    {
                        _session.Io.Output("Text to be replaced : ");
                        var search = _session.Io.InputLine();
                        _session.Io.OutputLine();
                        _session.Io.Output("Text to replace with: ");
                        var replace = _session.Io.InputLine();
                        _session.Io.OutputLine();
                        line = line.Replace(search, replace);
                    }
                    break;
                case 'R':
                    // re-type
                    _session.Io.OutputLine("Retype line (enter = keep existing)");
                    string l = _session.Io.InputLine();
                    if (string.IsNullOrWhiteSpace(l))
                        _session.Io.OutputLine("Aborting edit");
                    else
                        line = l;
                    break;
                default:
                    _session.Io.OutputLine("Aborting edit");
                    break;
            }

            return line;
        }

        private void Delete(List<string> lines, string args)
        {
            var range = string.IsNullOrWhiteSpace(args) ?
                new Tuple<int, int>(lines.Count, lines.Count) :
                ParseRange(args, lines.Count+1);

            int lineCount = range.Item2 - range.Item1 + 1;

            using (_session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
            {
                for (int i = range.Item1 - 1; i <= range.Item2 - 1 && i < lines.Count; i++)
                    _session.Io.OutputLine(lines[i]);
                _session.Io.SetForeground(ConsoleColor.Magenta);
                Ask($"Delete {(lineCount == 1 ? "this line" : "these lines")}?", new Dictionary<char, Action>
                {
                    {'y', () =>
                    {
                        for (int i=range.Item2-1; i>= range.Item1-1; i--)
                        {
                            lines.RemoveAt(i);
                        }
                        _session.Io.OutputLine($"{lineCount} line{(lineCount == 1 ? "" : "s")} deleted.");
                    } }
                });
            }
        }

        private void InsertLines(List<string> lines, string[] args)
        {
            if (!int.TryParse(args[0], out int insertPoint) || insertPoint < 0 || insertPoint >= lines.Count)
            {
                Notify("Invalid line number.");
                return;
            }
            int numLines = 1;
            if (args.Length >= 2 && int.TryParse(args[1], out int nl) && nl > 0 && nl <= Constants.MaxLinesToInsertInLineEditor)
                numLines = nl;

            for (int i = 0; i < numLines; i++)
                lines.Insert(insertPoint, string.Empty);

            Notify($"{numLines} blank lines inserted after line # {insertPoint}.");
        }

        private void List(IList<string> lines, bool withLineNumbers, string range=null)
        {
            string body;
            Tuple<int, int> rangeTuple = ParseRange(range, lines.Count+1);
            var builder = new StringBuilder();
            for (int i=rangeTuple.Item1-1; i < lines.Count && i+1 <= rangeTuple.Item2; i++)
                builder.AppendLine($"{(withLineNumbers ? $"{i + 1} : " : "")}{lines[i]}");
            body = builder.ToString();

            using (_session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Cyan))
            {
                _session.Io.OutputLine(body);
            }
        }

        private Tuple<int, int> ParseRange(string range, int upperLimit)
        {
            Func<int, int, Tuple<int, int>> MakeTuple = (_a, _b) =>
            {
                if (_a < 1) _a = 1;
                if (_a > upperLimit) _a = upperLimit;
                if (_b < 1) _b = 1;
                if (_b > upperLimit) _b = upperLimit;
                _a = Math.Min(_a, _b);
                _b = Math.Max(_a, _b);
                return new Tuple<int, int>(_a, _b);
            };

            if (string.IsNullOrWhiteSpace(range))
                return MakeTuple(1, upperLimit);
            if (int.TryParse(range, out int n))
            {
                if (n < 0)
                    return MakeTuple(1, -n);
                else if (n == 0)
                    return MakeTuple(1, 1);
                else
                    return MakeTuple(n, n);
            }
            var parts = range.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1 && int.TryParse(parts[0], out int a))
                return MakeTuple(a, upperLimit);
            if (parts.Length > 1 && int.TryParse(parts[0], out int a1) && int.TryParse(parts[1], out int b1))
                return MakeTuple(a1, b1);
            return MakeTuple(1, upperLimit);
        }

        private string Compile(IList<string> lines)
        {
            string body = string.Join(Environment.NewLine, lines);
            return body;
        }

        private void Ask(string question, IDictionary<char, Action> actions)
        {
            using (_session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
            {
                _session.Io.Output($"{question}: ");
                var c = _session.Io.InputKey();
                _session.Io.OutputLine();
                if (!c.HasValue)
                    return;
                char lowerC = Char.ToLower(c.Value);
                if (actions.ContainsKey(lowerC))
                    actions[lowerC].Invoke();
            }
        }

        private void Notify(string notification)
        {
            using (_session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                _session.Io.OutputLine($" >>> {notification} <<<");
            }
        }

        [Flags]
        private enum CommandResult
        {
            None = 0,

            /// <summary>
            /// Indicates that the user wishes to edit the editor
            /// </summary>
            ExitEditor = 1,

            /// <summary>
            /// Indicates that the user wishes to undo any edits and restore the text to the original
            /// </summary>
            RevertToOriginal = 2,

            /// <summary>
            /// Indicates that a save was performed and therefore, while exiting, will not be asked if 
            /// they should save
            /// </summary>
            Saved = 4
        }
    }
}
