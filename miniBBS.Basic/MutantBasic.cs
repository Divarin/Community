﻿using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Executors;
using miniBBS.Basic.Models;
using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using miniBBS.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace miniBBS.Basic
{
    public class MutantBasic : ITextEditor
    {
        private BbsSession _session;
        private string _loadedData;
        private LineEditorParameters _parameters;
        private string _rootDirectory;
        private bool _autoStart;
        private readonly bool _isScript;
        private readonly string _scriptName;
        private readonly string _scriptInput;
        private readonly bool _isDebugging;

        private static bool _debug = false;
        private bool _prompt = false;

        private static ConcurrentBag<WeakReference> _runningBasics = new ConcurrentBag<WeakReference>();
        public BasicStateInfo State { get; private set; } = new BasicStateInfo();

        public Func<string, string> OnSave { get; set; }

        public MutantBasic(string rootDirectory, bool autoStart, string scriptName = null, string scriptInput = null, bool isDebugging = false)
        {
            _rootDirectory = rootDirectory;
            _autoStart = autoStart;
            _isScript = !string.IsNullOrWhiteSpace(scriptName);
            _scriptName = scriptName;
            _scriptInput = scriptInput;
            _isDebugging = isDebugging;

            _runningBasics.Add(new WeakReference(this));
        }

        ~MutantBasic()
        {
            var mywr = _runningBasics.FirstOrDefault(x => x.IsAlive && ReferenceEquals(this, x.Target));
            if (mywr != null)
                _runningBasics.TryTake(out WeakReference result);
        }

        //public static bool TryAutoLaunch(string[] args)
        //{
        //    const string drop = "drop=";
        //    const string prog = "prog=";

        //    if (true != args?.Any(a => "basic".Equals(a, StringComparison.CurrentCultureIgnoreCase)))
        //        return false;
        //    var dropfile = args.FirstOrDefault(a => a.StartsWith(drop)).Substring(drop.Length);
        //    if (string.IsNullOrWhiteSpace(dropfile))
        //        return false;
        //    var programName = args.FirstOrDefault(a => a.StartsWith(prog)).Substring(prog.Length);
        //    if (string.IsNullOrWhiteSpace(programName))
        //        return false;
        //    BbsSession pseudoSession = SetupPseudoSession(dropfile);
        //    if (pseudoSession == null)
        //        return false;

        //    var basic = new MutantBasic(Constants.TextFileRootDirectory + $"(path here)", true, programName);

        //    var parameters = new LineEditorParameters
        //    {
        //        Filename = programName,
        //        PreloadedBody = // put basic code here
        //    };
        //    basic.EditText(pseudoSession, parameters);
        //}

        public void EditText(BbsSession session, LineEditorParameters parameters = null)
        {
            _session = session;
            _loadedData = parameters?.PreloadedBody;
            _parameters = parameters;

            State.Session = session;
            State.ProgramPath = $"{_rootDirectory}{parameters?.Filename}";

            Start();
        }

        private void Start()
        {
            var originalLocation = _session.CurrentLocation;
            _session.CurrentLocation = Module.BasicEditor;
            
            try
            {
                if (!_isScript)
                {
                    _session.Io.OutputLine("  ***** Mutant Basic  ***** ");
                    _session.Io.SetColors(ConsoleColor.Black, ConsoleColor.Cyan);
                }

                StartInternal();
            } 
            finally
            {
                Files.CloseAllFileHandlers(_session);
                _session.Io.AbortPollKey();
                _session.CurrentLocation = originalLocation;
            }
        }

        private void StartInternal()
        {
            bool autoRunComplete = false;

            if (!_autoStart)
            {
                State.IsInEditor = true;
                _session.Io.OutputLine("Programmer's Reference Guide is available at http://mutinybbs.com/mtbasic");
                _session.Io.SetForeground(ConsoleColor.Cyan);
                _session.Io.OutputLine("Type QUIT to return to BBS");
                _session.Io.OutputLine("Type HELP for quick reference");
                _session.Io.OutputLine("While your program is running, hold ESC to abort");
                _session.Io.SetForeground(ConsoleColor.Yellow);
                _session.Io.OutputLine();
                _session.Io.OutputLine($"Hello {_session.User.Name}, program me!");
                _session.Io.OutputLine();
                _session.Io.OutputLine();
                _session.Io.SetForeground(ConsoleColor.Green);
            }

            SortedList<int, string> progLines = ProgramData.Deserialize(_loadedData);

            var variables = new Variables(GetEnvironmentVaraibles(_session));
            
            if (!string.IsNullOrWhiteSpace(_loadedData))
                TryLoad(ref progLines, ref variables);

            bool quit = false;

            while (!quit)
            {
                string line = null;
                ProgramLine pline = null;
                if (_autoStart)
                {
                    if (autoRunComplete)
                    {
                        break;
                    }
                    else
                    {
                        line = "run";
                        autoRunComplete = true;
                    }
                }
                else
                {
                    if (_prompt)
                        _session.Io.Output("] ");
                    line = _session.Io.InputLine();
                    if (string.IsNullOrEmpty(line))
                        continue;
                    pline = GetProgramLine(line);
                }

                if (pline != null)
                {
                    if (string.IsNullOrEmpty(pline.Statement))
                        progLines.Remove(pline.LineNumber);
                    else
                        progLines[pline.LineNumber] = pline.Statement;
                }
                else if (GetReassignment(line, out int[] reassignemnt))
                {
                    int _oldLineNumber = reassignemnt[1];
                    int _newLineNumber = reassignemnt[0];
                    if (_oldLineNumber == _newLineNumber)
                        _session.Io.OutputLine("Well duh.");
                    else if (_newLineNumber < 0)
                        _session.Io.OutputLine($"? Illegal line number, {_newLineNumber}");
                    else if (!progLines.ContainsKey(_oldLineNumber))
                        _session.Io.OutputLine($"? Cannot copy line number {_oldLineNumber}, no such line.");
                    else
                    {
                        string orig = null;
                        if (progLines.ContainsKey(_newLineNumber))
                            orig = progLines[_newLineNumber];

                        progLines[_newLineNumber] = progLines[_oldLineNumber];
                        _session.Io.OutputLine($"Copied line {_oldLineNumber} to {_newLineNumber}.");
                        if (orig != null)
                        {
                            _session.Io.OutputLine($"Overwriting previous line {_newLineNumber}:");
                            _session.Io.OutputLine($"  {_newLineNumber} {orig}");
                        }
                    }
                }
                else if (line.Equals("debug", StringComparison.CurrentCultureIgnoreCase))
                {
                    _debug = !_debug;
                    _session.Io.OutputLine($"Debug={_debug}");
                }
                else if (line.StartsWith("renum", StringComparison.CurrentCultureIgnoreCase))
                {
                    progLines = Renum.Execute(line.Substring(5), progLines);
                }
                else if (line.StartsWith("list", StringComparison.CurrentCultureIgnoreCase))
                {
                    var range = Range.Parse(line.Substring(4), variables);
                    ListProgram(progLines, range, continuous: false);
                }
                else if (line.StartsWith("clist", StringComparison.CurrentCultureIgnoreCase))
                {
                    var range = Range.Parse(line.Substring(5), variables);
                    ListProgram(progLines, range, continuous: true);
                }
                else if (line.StartsWith("lI"))
                {
                    var range = Range.Parse(line.Substring(2), variables);
                    ListProgram(progLines, range, continuous: false);
                }
                else if (line.StartsWith("prompt", StringComparison.CurrentCultureIgnoreCase))
                {
                    _prompt = !_prompt;
                }
                else if (line.StartsWith("run", StringComparison.CurrentCultureIgnoreCase) && true == progLines?.Any())
                {
                    var _previousLocation = _session.CurrentLocation;
                    _session.CurrentLocation = Module.BasicInterpreter;

                    try
                    {
                        Run(ref progLines, ref variables, line);
                    }
                    finally
                    {
                        Files.CloseAllFileHandlers(_session);
                        State.IsRunning = false;
                        State.Variables = null;
                        _session.CurrentLocation = _previousLocation;
                        _session.Io.SetLower();
                        if (_session.Items.ContainsKey(SessionItem.BasicLock))
                            _session.Items.Remove(SessionItem.BasicLock);
                    }
                }
                else if (line.StartsWith("new", StringComparison.CurrentCultureIgnoreCase))
                {
                    var range = Range.Parse(line.Substring(3), variables)
                        ?.Where(x => x.HasValue)
                        ?.Select(x => x.Value)
                        ?.ToArray();
                    if (range?.Length == 1)
                    {
                        if (progLines.ContainsKey(range[0]))
                            progLines.Remove(range[0]);
                    }
                    else if (range?.Length == 2)
                    {
                        for (var i = range[1]; i >= range[0]; i--)
                            if (progLines.ContainsKey(i))
                                progLines.Remove(i);
                    }
                    else
                    {
                        _loadedData = string.Empty;
                        progLines = ProgramData.Deserialize(_loadedData);
                        variables = new Variables(GetEnvironmentVaraibles(_session));
                    }
                }
                else if (line.StartsWith("rem", StringComparison.CurrentCultureIgnoreCase))
                {
                    string s = line.Substring(3);
                    var range = Range.Parse(s, variables);
                    if (range[0].HasValue || range[1].HasValue)
                    {
                        int[] lineNumbers = progLines.Keys.ToArray();

                        foreach (var l in lineNumbers)
                        {
                            if (range[0].HasValue && l < range[0].Value)
                                continue;
                            if (range[1].HasValue && l > range[1].Value)
                                continue;
                            if (progLines.ContainsKey(l))
                                progLines[l] = "rem " + progLines[l];
                        }
                    }
                }
                else if (line.StartsWith("unrem", StringComparison.CurrentCultureIgnoreCase))
                {
                    string s = line.Substring(5);

                    var range = Range.Parse(s, variables);
                    if (range[0].HasValue || range[1].HasValue)
                    {
                        int[] lineNumbers = progLines.Keys.ToArray();

                        foreach (var l in lineNumbers)
                        {
                            if (range[0].HasValue && l < range[0].Value)
                                continue;
                            if (range[1].HasValue && l > range[1].Value)
                                continue;
                            if (progLines.ContainsKey(l) && progLines[l].Trim().StartsWith("rem", StringComparison.CurrentCultureIgnoreCase))
                            {
                                string _l = progLines[l].Trim();
                                _l = _l.Substring(4);
                                progLines[l] = _l;
                            }
                        }
                    }
                }                
                else if (line.Equals("vars", StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (var key in variables.Keys)
                        _session.Io.OutputLine($"{key} = {variables[key]}");
                }
                else if (line.Equals("labels", StringComparison.CurrentCultureIgnoreCase))
                {
                    ListLabels(progLines);
                }
                else if ("save".Equals(line, StringComparison.CurrentCultureIgnoreCase) || "/s".Equals(line, StringComparison.CurrentCultureIgnoreCase))
                {
                    _loadedData = ProgramData.Serialize(progLines);
                    OnSave(_loadedData);
                    _session.Io.OutputLine("program saved");
                }                
                else if (line.Equals("quit", StringComparison.CurrentCultureIgnoreCase) || line.Equals("/q", StringComparison.CurrentCultureIgnoreCase))
                {
                    quit = true;
                }
                else if (line.Equals("/sq", StringComparison.CurrentCultureIgnoreCase))
                {
                    _loadedData = ProgramData.Serialize(progLines);
                    OnSave(_loadedData);
                    _session.Io.OutputLine("program saved");
                    quit = true;
                }
                else if (line.StartsWith("help", StringComparison.CurrentCultureIgnoreCase))
                {
                    string topic = line.Substring(4)?.Replace("\"", "").Trim();
                    Help.Display(_session, topic);
                }
                else if (line.StartsWith("find ", StringComparison.CurrentCultureIgnoreCase))
                {
                    string term = line.Substring(5)?.Replace("\"", "")?.Trim()?.ToLower();
                    Find.Execute(_session, term, progLines);
                }
                else if (line.StartsWith("edit ", StringComparison.CurrentCultureIgnoreCase))
                {
                    var edited = Edit.Execute(_session, line.Substring(5), progLines);
                    if (!edited.Aborted)
                    {
                        progLines[edited.LineNumber] = edited.NewLine;
                        _session.Io.OutputLine($"Line {edited.LineNumber}, Original:");
                        _session.Io.OutputLine(edited.OriginalLine);
                        _session.Io.OutputLine("Edited:");
                        _session.Io.OutputLine(edited.NewLine);
                    }
                }
                else
                {
                    try
                    {
                        variables.Data = Data.CreateFromDataStatements(progLines.Values);

                        foreach (var statement in GetStatements(line))
                            Execute(statement, variables);
                    }
                    catch (Exception ex)
                    {
                        _session.Io.OutputLine($"\n? {ex.Message}");
                    }

                }
            }
        }

        private void ListLabels(SortedList<int, string> progLines)
        {
            var builder = new StringBuilder();
            foreach (var line in progLines)
            {
                if (line.Value.StartsWith("!"))
                    builder.AppendLine($"{line.Key} {line.Value}");
            }
            _session.Io.Output(builder.ToString());
        }

        private void ListProgram(SortedList<int, string> progLines, int?[] range, bool continuous)
        {
            const OutputHandlingFlag flag = OutputHandlingFlag.SplitOnNewlineOnly;

            var builder = new StringBuilder();
            foreach (var l in progLines)
            {
                if (range[0].HasValue && l.Key < range[0].Value)
                    continue;
                if (range[1].HasValue && l.Key > range[1].Value)
                    continue;
                var line = $"{l.Key} {l.Value}";
                if (continuous)
                    _session.Io.OutputLine(line, flag);
                else
                    builder.AppendLine(line);
            }
            if (!continuous)
                _session.Io.Output(builder.ToString(), flag);
        }

        private void Run(ref SortedList<int, string> progLines, ref Variables variables, string line)
        {
            if (progLines == null || progLines.Count < 1)
            {
                _session.Io.OutputLine("No program in memory");
                return;
            }

            StatementPointer sp = new StatementPointer();
            StatementPointer lastLine = new StatementPointer();
            {
                var _ll = progLines.Last();
                var last = GetStatements(_ll.Value);
                lastLine.LineNumber = _ll.Key;
                lastLine.StatementNumber = last.Count - 1;
            }
            bool brk = false;
            var range = Range.Parse(line.Substring(3), variables);
            variables.ClearScoped();
            variables.Labels = FindLabels(progLines);
            _session.Io.AbortPollKey();
            _session.Io.ClearPolledKey();
            
            var shouldPollKeys = !_isScript && !"polloff".Equals(progLines.Values.First(), StringComparison.CurrentCultureIgnoreCase);
            if (shouldPollKeys)
                _session.Io.PollKey();

            int lastLineNumber = -1;
            List<string> currentLineStatements = null;

            variables.Data = Data.CreateFromDataStatements(progLines.Values);
            variables.Functions.Clear();

            State.IsRunning = true;
            State.Variables = variables;
            var runTimer = Stopwatch.StartNew();
            while (true)
            {
                // break conditions
                if (brk)
                    break;
                if (sp.LineNumber < 0)
                    break;
                if (sp.LineNumber > lastLine.LineNumber)
                    break;
                if (sp.LineNumber == lastLine.LineNumber && sp.StatementNumber > lastLine.StatementNumber)
                    break;

                bool ranTooLong =
                    (_isScript && runTimer.Elapsed.TotalSeconds > 5) ||
                    runTimer.Elapsed.TotalMinutes > Constants.BasicMaxRuntimeMin;

                if (ranTooLong)
                {
                    _session.Io.OutputLine("Maximum program runtime reached.");
                    break;
                }

                if (!_isScript)
                {
                    var polledKey = _session.Io.GetPolledKey();
                    if (polledKey == (char)27 || polledKey == '\u0003') // escape or ctrl+c
                    {
                        brk = true;
                        _session.Io.AbortPollKey();
                        _session.Io.OutputLine();
                        _session.Io.OutputLine($"? break at line {sp.LineNumber}");
                    }
                }

                if (!progLines.ContainsKey(sp.LineNumber) ||
                    range[0].HasValue && sp.LineNumber < range[0].Value ||
                    range[1].HasValue && sp.LineNumber > range[1].Value)
                {
                    sp.LineNumber++;
                    continue;
                }

                try
                {
                    if (sp.LineNumber != lastLineNumber)
                    {
                        lastLineNumber = sp.LineNumber;
                        currentLineStatements = GetStatements(progLines[sp.LineNumber]);
                    }
                    string statement = currentLineStatements[sp.StatementNumber];
                    sp = Execute(statement, variables, sp, sp.StatementNumber < currentLineStatements.Count - 1, progLines);
                }
                catch (RuntimeException rex)
                {
                    if (rex.ExceptionLocation.LineNumber >= 0)
                        _session.Io.OutputLine($"Error in line {rex.ExceptionLocation.LineNumber} statement number {rex.ExceptionLocation.StatementNumber + 1}");
                    _session.Io.OutputLine($"? {rex.Message}");
                    brk = true;
                }
            }

            State.IsRunning = false;
            
            if (!_isScript)
            {
                var wasPolling = _session.Io.IsPollingKeys;
                _session.Io.AbortPollKey();
                _session.Io.OutputLine($"Program run complete{(wasPolling ? ", press any key..." : "")}.");
                if (wasPolling)
                    Thread.Sleep(2000);
            }
        }

        private void TryLoad(ref SortedList<int, string> progLines, ref Variables variables)
        {
            if (string.IsNullOrWhiteSpace(_loadedData))
                _session.Io.OutputLine("? nothing to load");
            else
            {
                progLines = ProgramData.Deserialize(_loadedData);
                variables = new Variables(GetEnvironmentVaraibles(_session))
                {
                    Labels = FindLabels(progLines)
                };
                if (!_isScript)
                    _session.Io.OutputLine("program loaded");
            }
        }

        public static IEnumerable<Variables> GetVariablesOfOtherSessionsRunningThisProgram(BasicStateInfo callingState)
        {
            foreach (var wr in _runningBasics)
            {
                if (!wr.IsAlive)
                    continue;
                if (!(wr.Target is MutantBasic mb) || mb.State == null || !mb.State.IsRunning || mb._session == null)
                    continue;
                if (mb.State.Session?.Id == callingState.Session?.Id)
                    continue;
                if (mb.State.ProgramPath != callingState.ProgramPath)
                    continue;
                yield return mb.State.Variables;
            }
        }

        private static bool GetReassignment(string line, out int[] reassignemnt)
        {
            reassignemnt = null;

            if (true != line?.Contains("="))
                return false;
            var parts = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return false;
            int n, o;
            if (!int.TryParse(parts[0], out n) || !int.TryParse(parts[1], out o))
                return false;

            reassignemnt = new int[] { n, o };
            return true;
        }

        private static List<string> GetStatements(string line)
        {
            List<string> statements = new List<string>();

            StringBuilder builder = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    builder.Append(c);
                }
                else if (!inQuotes && c == ':')
                {
                    statements.Add(builder.ToString());
                    builder.Clear();
                }
                else
                {
                    builder.Append(c);
                    if (c == '?' && !inQuotes && i < line.Length - 1 && line[i + 1] != ' ' && line[i + 1] != '?')
                        builder.Append(' ');
                }
            }
            if (builder.Length > 0)
                statements.Add(builder.ToString());

            return statements;
        }

        private IDictionary<string, Func<string>> GetEnvironmentVaraibles(BbsSession session)
        {
            var vars = new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase);
            vars["USERNAME$"] = () => '"' + session.User.Name + '"';
            vars["USERID"] = () => session.User.Id.ToString();
            vars["SESSIONID$"] = () => '"' + $"{session.Id}" + '"';
            vars["EMULATION$"] = () => '"' + session.User.Emulation.ToString() + '"';
            vars["TERMROWS"] = () => session.Rows.ToString();
            vars["TERMCOLS"] = () => session.Cols.ToString();
            vars["DATE$"] = () => '"' + DateTime.Now.AddHours(session.TimeZone).ToString("MM/dd/yyyy") + '"';
            vars["TIME$"] = () => '"' + DateTime.Now.AddHours(session.TimeZone).ToString("HH:mm:ss") + '"';
            vars["TICKS"] = () => DateTime.Now.Ticks.ToString();
            vars["INKEY$"] = () =>
            {
                var key = _session.Io.GetPolledKey();
                if (key.HasValue)
                    return '"' + ((char)key).ToString() + '"';
                return "\"\"";
            };
            vars["INKEY"] = () => _session.Io.GetPolledTicks().ToString();

            if (_isScript)
            {
                vars["SCRIPTNAME$"] = () => '"' + _scriptName + '"';
                vars["CHAT$"] = () => '"' + _scriptInput + '"';
                vars["DEBUGGING"] = () => _isDebugging ? "1" : "0";
                vars["CHANNEL$"] = () => _session.Channel.Name;
            }

            return vars;
        }

        private Dictionary<string, int> FindLabels(SortedList<int, string> progLines)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
            foreach (var line in progLines)
            {
                if (line.Value.StartsWith("!") && line.Value.Length > 1)
                {
                    string label = line.Value.Substring(1);
                    if (dict.ContainsKey(label))
                    {
                        _session.Io.OutputLine($"dupliate labels: {label} is on lines {dict[label]} and {line.Key}.");
                        _session.Io.Output(" [ press any key ]");
                        _session.Io.InputKey();
                        _session.Io.OutputLine();
                    }
                    else
                        dict[label] = line.Key;
                }
            }
            return dict;
        }

        private ProgramLine GetProgramLine(string line)
        {
            int pos = line.IndexOf(' ');
            if (pos <= 0)
            {
                if (int.TryParse(line, out int i))
                    return new ProgramLine(i, null);
                return null;
            }
            int lineNumber;
            string strLineNumber = line.Substring(0, pos)?.Trim();
            string statement = line.Substring(pos)?.Trim();
            if (int.TryParse(strLineNumber, out lineNumber))
            {
                if (lineNumber < 0)
                    _session.Io.OutputLine("? smallest line number is 0, use RENUM if needed.");
                else
                    return new ProgramLine(lineNumber, statement);
            }
            return null;
        }

        /// <summary>
        /// wrapper to call Execute from immediate
        /// </summary>
        private void Execute(string statement, Variables variables)
        {
            Execute(statement, variables, new StatementPointer() { LineNumber = -1 }, false);
        }

        /// <summary>
        /// Executes the statement and returns the next statement pointer to (try to) execute.  This is normally will be the next statement after <paramref name="fromSp"/>
        /// but statements such as GOTO, GOSUB, NEXT, and RETURN, and IF may change that.
        /// </summary>
        private StatementPointer Execute(
            string statement,
            Variables variables,
            StatementPointer fromSp,
            bool currentLineHasMoreStatements,
            SortedList<int, string> progLines = null)
        {
            StatementPointer nextSp;
            if (currentLineHasMoreStatements)
                nextSp = new StatementPointer { LineNumber = fromSp.LineNumber, StatementNumber = fromSp.StatementNumber + 1 };
            else
                nextSp = new StatementPointer { LineNumber = fromSp.LineNumber + 1, StatementNumber = 0 };

            statement = statement.TrimStart();

            if (statement.StartsWith("!"))
                return nextSp; // skip label

            string command, args;

            int pos = statement.IndexOf(' ');
            if (pos > 0)
            {
                command = statement.Substring(0, pos)?.Trim();
                args = statement.Substring(pos)?.Trim();
            }
            else
            {
                command = statement;
                args = null;
            }

            try
            {
                switch (command.ToLower())
                {
                    case "lock":
                        _session.Items[SessionItem.BasicLock] = $"{_rootDirectory}{_parameters?.Filename}";
                        break;
                    case "share":
                        Share.Execute(_session, variables, State, args);
                        break;
                    case "?":
                    case "??":
                    case "debug":
                    case "print":
                    case "uprint":
                        if (!_debug && (command.Equals("debug", StringComparison.CurrentCultureIgnoreCase) || command.Equals("??")))
                            break;
                        if (_isScript)
                        {
                            if (!_autoStart || "uprint".Equals(command, StringComparison.CurrentCultureIgnoreCase))
                                Print.Execute(_session, args, variables);
                            else
                                Print.BroadcastToChannel(_session, args, variables);
                            return new StatementPointer() { LineNumber = -1, StatementNumber = 0 }; // end
                        }
                        else
                        {
                            Print.Execute(_session, args, variables);
                        }  
                        break;
                    case "let":
                        Let.Execute(_session, args, variables, _rootDirectory);
                        break;
                    case "dim":
                        Dim.Execute(_session, args, variables);
                        break;
                    case "goto":
                        {
                            int ln = Goto.Execute(this._session, args, variables);
                            nextSp.LineNumber = ln;
                            nextSp.StatementNumber = 0;
                        }
                        break;
                    case "if":
                        {
                            string s = If.Execute(this._session, args, variables);
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                nextSp = Execute(s, variables, fromSp, currentLineHasMoreStatements, progLines);
                            }
                            else if (currentLineHasMoreStatements)
                            {
                                nextSp.LineNumber++;
                                nextSp.StatementNumber = 0;
                            }
                        }
                        break;
                    case "color":
                        Color.Execute(_session, args, variables);
                        break;
                    case "for":
                        {
                            if (fromSp.LineNumber < 0)
                                throw new RuntimeException("cannot be used in immediate mode.");
                            else
                            {
                                For f = new For();
                                if (f.Execute(this._session, args, variables, nextSp))
                                {
                                    var variableName = f.VariableName;
                                    if (variables.PeekAllScoped()?.FirstOrDefault(x => x is For && (x as For).VariableName == variableName) is For existing)
                                    {
                                        f = existing; // re-entered for loop without exiting the first time (probably goto above the FOR)
                                    }
                                    else
                                    {
                                        variables.PushScoped(f);
                                    }
                                }
                            }
                        }
                        break;
                    case "next":
                        {
                            For f = variables.PeekScoped() as For;
                            if (f == null)
                                _session.Io.OutputLine("? next without for");
                            else
                            {
                                if (f.Advance(args))
                                    nextSp = f.FirstStatementPosition;
                                else
                                    variables.PopScoped();
                            }
                        }
                        break;
                    case "gosub":
                        {
                            Gosub g = new Gosub();
                            variables.PushScoped(g);
                            int _gosubLineNum = g.Execute(this._session, args, variables, nextSp);
                            nextSp.LineNumber = _gosubLineNum;
                            nextSp.StatementNumber = 0;
                        }
                        break;
                    case "on":
                        {
                            var result = On.Execute(this._session, args, variables);
                            if (result.Success)
                            {
                                if (result.Gosub)
                                {
                                    // GOSUB
                                    Gosub g = new Gosub();
                                    variables.PushScoped(g);
                                    int _gosubLineNum = g.Execute(result.LineNumber, nextSp);
                                    nextSp.LineNumber = _gosubLineNum;
                                    nextSp.StatementNumber = 0;
                                }
                                else
                                {
                                    // GOTO
                                    nextSp.LineNumber = result.LineNumber;
                                    nextSp.StatementNumber = 0;
                                }
                            }
                        }
                        break;
                    case "return":
                        {
                            Gosub g = variables.PeekScoped() as Gosub;
                            if (g == null)
                                throw new RuntimeException($"RETURN without GOSUB in {fromSp.LineNumber}");
                            else
                            {
                                variables.PopScoped();
                                nextSp = g.ReturnToPosition;
                            }
                        }
                        break;
                    case "input":
                        if (!_isScript)
                            Input.Execute(_session, args, variables);
                        break;
                    case "get":
                        if (!_isScript)
                            Get.Execute(_session, args, variables);
                        break;
                    case "end":
                        return new StatementPointer() { LineNumber = -1, StatementNumber = 0 };
                    case "rem":
                        // ignore this statement and any other statements on the current line.
                        nextSp.LineNumber++;
                        nextSp.StatementNumber = 0;
                        break;
                    case "cls":
                        _session.Io.ClearScreen();
                        break;
                    case "clr":
                        if (!string.IsNullOrWhiteSpace(args))
                        {
                            foreach (var arg in args.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                                variables.Remove(arg);
                        }
                        else
                            variables.Clear();
                        break;
                    case "home":
                        Position.Home(_session);
                        break;
                    case "position":
                        Position.Execute(_session, args, variables);
                        break;
                    case "up":
                        Position.Up(_session, args, variables);
                        break;
                    case "down":
                        Position.Down(_session, args, variables);
                        break;
                    case "left":
                        Position.Left(_session, args, variables);
                        break;
                    case "right":
                        Position.Right(_session, args, variables);
                        break;
                    case "petsciiupper":
                        _session.Io.SetUpper();
                        break;
                    case "petsciilower":
                        _session.Io.SetLower();
                        break;
                    case "randomize":
                        Rnd.SetSeed(this._session, args, variables);
                        break;
                    case "pollon":
                        if (!_isScript)
                            _session.Io.PollKey();
                        break;
                    case "polloff":
                        if (!_isScript)
                            _session.Io.AbortPollKey();
                        break;
                    case "@":
                    case "sql":
                        new Sql(_rootDirectory).Execute(_session, args, variables);
                        break;
                    case "savestate":
                        new Sql(_rootDirectory).ExecuteStateCommand(_session, "savestate", args, variables);
                        break;
                    case "loadstate":
                        new Sql(_rootDirectory).ExecuteStateCommand(_session, "loadstate", args, variables);
                        break;
                    case "clearstate":
                        new Sql(_rootDirectory).ExecuteStateCommand(_session, "clearstate", args, variables);
                        break;
                    case "states":
                        new Sql(_rootDirectory).ExecuteStateCommand(_session, "states", null, variables);
                        break;
                    case "data":
                        // ignore data line
                        break;
                    case "read":
                        variables.Data.Read(this._session, args, variables);
                        break;
                    case "restore":
                        variables.Data.Restore();
                        break;
                    case "def":
                        {
                            Function function = Function.Create(args);
                            variables.Functions.Add(function);
                        }
                        break;
                    case "resetnextword":
                        Words.ResetNextWord();
                        break;
                    case "wait":
                        {
                            if (!string.IsNullOrWhiteSpace(args) && int.TryParse(args, out var delay) && delay > 0 && delay < 60000)
                                Thread.Sleep(delay);
                        }
                        break;
                    case "showfile":
                        Files.ShowFile(_session, _rootDirectory, args, variables);
                        break;
                    case "open#":
                        Files.Open(_session, _rootDirectory, string.Join(" ", args), variables, false);
                        break;
                    case "openu#":
                        Files.Open(_session, _rootDirectory, string.Join(" ", args), variables, true);
                        break;
                    case "print#":
                        Files.Print(_session,  string.Join(" ", args), variables);
                        break;
                    case "close#":
                        Files.Close(_session, string.Join(" ", args), variables);
                        break;
                    case "get#":
                        Files.Get(_session, string.Join(" ", args), variables);
                        break;
                    case "input#":
                        Files.Input(_session, string.Join(" ", args), variables);
                        break;
                    case "seek#":
                        Files.Seek(_session, string.Join(" ", args), variables);
                        break;
                    case "files#":
                        Files.ShowHandlers(_session);
                        break;
                    case "notify" when _session.User.Access.HasFlag(AccessFlag.Administrator):
                        Notify.Execute(_session, args, variables);
                        break;
                    default:
                        if (statement.Contains('=') && !statement.StartsWith("LET"))
                        {
                            // try implicit LET
                            statement = "LET " + statement;
                            return Execute(statement, variables, fromSp, currentLineHasMoreStatements, progLines);
                        }
                        _session.Io.OutputLine($"? I don't know what '{statement}' means{(fromSp.LineNumber >= 0 ? " in line " + fromSp.LineNumber : "")}.");
                        break;
                }

            }
            catch (RuntimeException rex)
            {
                rex.ExceptionLocation = fromSp;
                throw;
            }
            return nextSp;
        }

        public static IEnumerable<string> CheckPermissions(string body)
        {
            SortedList<int, string> progLines = ProgramData.Deserialize(body);
            HashSet<string> permissions = new HashSet<string>();

            foreach (var line in progLines.Values)
            {
                var statements = GetStatements(line);
                foreach (var statement in statements)
                {
                    if (statement.Trim().StartsWith("openu#", StringComparison.OrdinalIgnoreCase))
                        permissions.Add("Open files for reading and/or writing in YOUR user directory.");
                }
            }

            return permissions;
        }

        public static string Decompress(string body)
        {
            try
            {
                var json = GlobalDependencyResolver.Default.Get<ICompressor>().Decompress(body);
                var dict = JsonConvert.DeserializeObject<Dictionary<int, string>>(json);
                var result = string.Join(Environment.NewLine, dict.Select(x => $"{x.Key} {x.Value}"));
                return result;
            }
            catch
            {
                return body;
            }
        }
    }
}
