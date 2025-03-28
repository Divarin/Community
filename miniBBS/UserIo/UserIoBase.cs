using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Exceptions;
using miniBBS.Extensions;
using miniBBS.Interfaces;
using miniBBS.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace miniBBS.UserIo
{
    public abstract class UserIoBase : IUserIo
    {
        protected ConsoleColor _currentForeground = ConsoleColor.White;
        protected ConsoleColor _currentBackground = ConsoleColor.Black;
        protected readonly BbsSession _session;
        protected readonly ILogger _logger;
        protected Queue<Action> _delayedNotifications = new Queue<Action>();
        protected Thread _delayedNotificationThread = null;

        private Stack<string> _lastLineStack = new Stack<string>();
        private Stack<string> _nextLineStack = new Stack<string>();

        public virtual string NewLine => Environment.NewLine;

        public UserIoBase(BbsSession session)
        {
            _session = session;
            _logger = DI.Get<ILogger>();
            
        }

        public abstract TerminalEmulation EmulationType { get; }

        public virtual bool IsInputting { get; protected set; }

        public virtual void DelayNotification(Action action)
        {
            _delayedNotifications.Enqueue(action);
            if (_delayedNotificationThread == null || !_delayedNotificationThread.IsAlive)
            {
                _delayedNotificationThread = new Thread(new ThreadStart(StartDelayedNotificationsTimer));
                _delayedNotificationThread.Start();
            }
        }

        public virtual void OutputRaw(params byte[] bytes)
        {
            if (_session.Stream.CanWrite)
            {
                _session.Stream.Write(bytes, 0, bytes.Length);
            }
        }

        public virtual void Output(char c)
        {
            StreamOutput(_session, $"{c}", OutputHandlingFlag.None);
        }

        public virtual void Output(string s, OutputHandlingFlag flags = OutputHandlingFlag.None)
        {
            StreamOutput(_session, s, flags);
        }

        public virtual void OutputLine(string s = null, OutputHandlingFlag flags = OutputHandlingFlag.None)
        {
            StreamOutputLine(_session, text:s, flags: flags);
        }

        public virtual void OutputBackspace()
        {
            Output("\b");
        }

        public virtual char? InputKey()
        {
            return StreamInput(_session);
        }

        public virtual string InputLine(InputHandlingFlag handlingFlag = InputHandlingFlag.None)
        {
            return StreamInputLine(_session, null, handlingFlag);
        }

        public virtual string InputLine(Func<string, string> autoComplete, InputHandlingFlag handlingFlag = InputHandlingFlag.None)
        {
            return StreamInputLine(_session, autoComplete, handlingFlag);
        }

        public abstract void ClearLine();
        public abstract void ClearScreen();
        public virtual void ResetColor()
        {
            if (_currentBackground != ConsoleColor.Black || _currentForeground != ConsoleColor.White)
            {
                SetColors(ConsoleColor.Black, ConsoleColor.White);
            }
        }

        public virtual void SetForeground(ConsoleColor color)
        {
            if (_currentForeground != color)
            {
                string ansi = GetForegroundString(color);
                _currentForeground = color;
                OutputRaw(Encoding.ASCII.GetBytes(ansi));
            }
        }

        public virtual void SetBackground(ConsoleColor color)
        {
            if (_currentBackground != color)
            {
                string ansi = GetBackgroundString(color);
                _currentBackground = color;
                OutputRaw(Encoding.ASCII.GetBytes(ansi));
            }
        }

        public virtual void SetColors(ConsoleColor background, ConsoleColor foreground)
        {
            SetBackground(background);
            SetForeground(foreground);
        }

        /// <summary>
        /// Sets the background and foreground colors.  When disposed (end of the using block) the original colors are restored
        /// </summary>
        public virtual IDisposable WithColorspace(ConsoleColor background, ConsoleColor foreground)
        {
            ColorSpace colorSpace = new ColorSpace(this, _currentBackground, _currentForeground);
            SetColors(background, foreground);
            return colorSpace;
        }

        public virtual ConsoleColor GetForeground()
        {
            return _currentForeground;
        }

        public virtual ConsoleColor GetBackground()
        {
            return _currentBackground;
        }

        public abstract string Bold { get; }
        public abstract string Underline { get; }
        public abstract string Reversed { get; }
        public abstract string NotReversed { get; }

        public abstract string Up { get; }
        public abstract string Down { get; }
        public abstract string Left { get; }
        public abstract string Right { get; }
        public abstract string Home { get; }
        public abstract string Clear { get; }

        public abstract void SetPosition(int x, int y);

        protected abstract string GetBackgroundString(ConsoleColor color);
        protected abstract string GetForegroundString(ConsoleColor color);

        public class ColorSpace : IDisposable
        {
            private readonly IUserIo _userIo;
            private readonly ConsoleColor _originalBackground;
            private readonly ConsoleColor _originalForeground;

            public ColorSpace(IUserIo userIo, ConsoleColor originalBackground, ConsoleColor originalForeground)
            {
                _userIo = userIo;
                _originalBackground = originalBackground;
                _originalForeground = originalForeground;
            }

            public void Dispose()
            {
                _userIo.SetColors(_originalBackground, _originalForeground);
            }
        }

        private PauseResult Pause(BbsSession session, KeywordSearch keywordSearch, double? percentage = null)
        {
            //             123456789012345678901234567890123456                       789|            1234567890123456789012345678901234567890
            string options = 
                $"{NewLine}(y)es, (n)o, (c)ontinuous, page (u)p{(session.Cols >= 80 ? ", " : NewLine)}(/) search, (s)ave for later, 1-9) x10%";

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.DarkRed))
            {
                char? k = null;

                do
                {
                    string more = NewLine + "[more? (?=more options)]";
                    if (percentage.HasValue)
                        more += $" {Math.Round(100 * percentage.Value, 0)}% ";

                    if (session.Io.EmulationType == TerminalEmulation.Cbm)
                        more = more.ToUpper();

                    var r = GetBytes(more);
                    session.Stream.Write(r, 0, r.Length);
                    k = StreamInput(session);
                    if (k == '?')
                    {
                        r = GetBytes(options);
                        session.Stream.Write(r, 0, r.Length);
                        continue;
                    }
                    break;
                } while (true);

                if (k != '/')
                {
                    var nl = GetBytes(NewLine);
                    session.Stream.Write(nl, 0, nl.Length);
                }

                if (k == 'c' || k == 'C')
                    return PauseResult.Continuous;
                else if (k == 'n' || k == 'N')
                    return PauseResult.No;
                else if (k == 'u' || k == 'U' || k == 27 || k == 'p' || k == 'P')
                    return PauseResult.PageUp;
                else if (k == '/')
                {
                    StreamOutput(session, TransformText("Search: "));
                    string search = StreamInputLine(session);
                    if (!string.IsNullOrWhiteSpace(search))
                        keywordSearch.Keyword = search;
                    keywordSearch.From = SearchFrom.AfterPreviousMatch_WithoutWraparound;
                    return PauseResult.ExecuteKeywordSearch;
                }
                else if (k == 's' || k == 'S')
                {
                    return PauseResult.Bookmark;
                }
                else if (int.TryParse($"{k}", out var n))
                {
                    double pcent = n / 10.0;
                    session.Items[SessionItem.BookmarkPercentage] = pcent;
                    return PauseResult.SetPercentage;
                }
            }
            return PauseResult.Yes;
        }

        protected virtual byte[] InterpretInput(byte[] arr)
        {
            return arr;
        }

        public virtual string TransformText(string text)
        {
            return text;
        }

        protected virtual void RemoveInvalidInputCharacters(ref byte[] bytes)
        {
            for (int i=0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                if (b == 13 || b == 10 || b == 8)
                    continue;
                if (b == 27 && i < bytes.Length - 3)
                {
                    bytes[i] = 0;
                    bytes[i + 1] = 0;
                    bytes[i + 2] = 0;
                }
                if (b < 32 || b > 127)
                    bytes[i] = 0;
            }
        }

        #region StreamIO
        protected virtual void StreamOutput(BbsSession session, string text, OutputHandlingFlag flags = OutputHandlingFlag.None)
        {
            bool continuousOutput = flags.HasFlag(OutputHandlingFlag.Nonstop);
            text = ReplaceInlineColors(text, out int actualTextLength);
            double? advPcent =
                session.Items.ContainsKey(SessionItem.BookmarkPercentage)
                ? session.Items[SessionItem.BookmarkPercentage] as double?
                : null;

            if (!session.Stream.CanWrite)
                return;

            if (actualTextLength <= session.Cols)
            {
                text = text.Replace(Constants.Spaceholder, ' ');
                var r = GetBytes(text);
                session.Stream.Write(r, 0, r.Length);
                return;
            }

            // handle wordwrap and pause

            List<string> lines = flags.HasFlag(OutputHandlingFlag.NoWordWrap) ? 
                new List<string>() { text } :
                text.SplitAndWrap(session, flags).ToList();

            // if the last line is just a newline and the line before that also ends with a newline then omit the last line
            // this is because splitAndWrap added an extra newline as a side-effect
            if (lines.Count >= 2 && lines.Last().Equals(NewLine) && lines[lines.Count - 2].EndsWith(NewLine))
                lines = lines.Take(lines.Count - 1).ToList();
            if (flags.HasFlag(OutputHandlingFlag.PauseAtEnd))
                lines.Add(TransformText(" --- End of Document ---"));
            int totalLines = lines.Count;
            int row = 0;
            var keywordSearch = new KeywordSearch();

            var pageMarkers = new Stack<int>();
            pageMarkers.Push(0);

            Func<int, int> DoPageUp = (_l) =>
            {
                if (pageMarkers.Count > 1)
                {
                    pageMarkers.Pop(); // top of current page
                    _l = pageMarkers.Pop(); // top of previous page
                    if (_l > 0)
                        _l--;
                    row = 0;
                    return _l;
                }
                else
                {
                    // restart top of document
                    _l = row = 0;
                    pageMarkers.Clear();
                    pageMarkers.Push(0);
                    return _l;
                }
            };
                        
            for (int l = 0; l <= lines.Count; l++)
            {
                row++;
                var line = l < lines.Count ? lines[l] : null;
                var pcent = (double)l / totalLines;

                // start pause
                if (!continuousOutput && (row >= session.Rows-3) || (l >= lines.Count && flags.HasFlag(OutputHandlingFlag.PauseAtEnd)))
                {
                    PauseResult pauseResult;
                    do
                    {
                        pauseResult = 
                            flags.HasFlag(OutputHandlingFlag.AdvanceToPercentage)
                            ? PauseResult.Yes
                            : Pause(session, keywordSearch, pcent);

                        if (pauseResult == PauseResult.Bookmark)
                        {
                            BookmarkManager.SaveBookmark(session, text, pcent, flags);
                            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
                            {
                                StreamOutputLine(session, TransformText($"Bookmarked, use /bookmark from chat in the future to recall."));
                            }
                        }
                    } while (pauseResult == PauseResult.Bookmark);

                    if (pauseResult == PauseResult.No)
                        break;
                    else if (pauseResult == PauseResult.Continuous)
                        continuousOutput = true;
                    else if (pauseResult == PauseResult.PageUp)
                    {
                        l = DoPageUp(l);
                        continue;
                    }
                    else if (pauseResult == PauseResult.SetPercentage)
                    {
                        row = 0;
                        l = 0;
                        advPcent =
                            session.Items.ContainsKey(SessionItem.BookmarkPercentage)
                            ? session.Items[SessionItem.BookmarkPercentage] as double?
                            : 0;

                        if (advPcent > 0)
                            flags |= OutputHandlingFlag.AdvanceToPercentage;
                        
                        continue;
                    }
                    else if (pauseResult == PauseResult.ExecuteKeywordSearch && !string.IsNullOrWhiteSpace(keywordSearch.Keyword))
                    {
                        int f = lines
                            .Skip(l) // so that we only search lines we haven't read yet
                            .IndexOf(x => x.ToLower().Contains(keywordSearch.Keyword.ToLower()))
                            + l; // because we did Skip(l) 
                        if (f > l)
                        {
                            if (f > 1) f -= 2; // back up a couple lines to provide context
                            l = f - 1; // set line number to line where we found the match (-1 because of the l++ coming up)
                        }
                        else
                        {
                            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
                            {
                                StreamOutputLine(session, TransformText($"No more occurances of '{keywordSearch.Keyword}' found."));
                            }
                            l = DoPageUp(l);
                            continue;
                        }
                    }
                    pageMarkers.Push(l);
                    row = 0;
                }
                // end pause

                if (flags.HasFlag(OutputHandlingFlag.AdvanceToPercentage))
                {
                    if (pcent >= advPcent)
                    {
                        flags &= ~OutputHandlingFlag.AdvanceToPercentage;
                        session.Items.Remove(SessionItem.BookmarkPercentage);
                        l = DoPageUp(l);
                    }
                    continue;
                }
                
                if (line != null)
                {
                    var r = GetBytes(line);
                    session.Stream.Write(r, 0, r.Length);
                }
            }
        }

        protected abstract string ReplaceInlineColors(string line, out int actualTextLength);

        public virtual byte[] GetBytes(string text)
        {
            return text.Select(c => (byte)c).ToArray();
            //return Encoding.ASCII.GetBytes(text);
        }

        protected virtual string GetString(byte[] bytes) => Encoding.ASCII.GetString(bytes);

        protected virtual string GetString(byte[] bytes, int index, int count) => Encoding.ASCII.GetString(bytes, index, count);

        protected virtual void StreamOutput(BbsSession session, params char[] characters)
        {
            string s = new string(characters, 0, characters.Length);
            //Console.Write(s);
            if (session.Stream.CanWrite)
            {
                var arr = characters.Select(c => (byte)c).ToArray();
                session.Stream.Write(arr, 0, arr.Length);
            }
        }

        protected virtual void StreamOutputLine(BbsSession session, OutputHandlingFlag flags = OutputHandlingFlag.None)
        {
            StreamOutput(session, NewLine, flags);
        }

        protected virtual void StreamOutputLine(BbsSession session, string text, OutputHandlingFlag flags = OutputHandlingFlag.None)
        {
            StreamOutput(session, text:$"{text}{NewLine}", flags);
        }

        public virtual byte[] InputRaw()
        {
            if (_session.Stream.CanRead && _session.Stream.CanWrite)
            {
                byte[] buffer = new byte[1024];
                var bytesRead = _session.Stream.Read(buffer, 0, buffer.Length);
                var result = buffer.Take(bytesRead).ToArray();
                return result;
            }
            return new byte[] { };
        }

        protected virtual char? StreamInput(BbsSession session)
        {
            if (_pollingOn)
            {
                _ = TryGetKeyFromPoll(out var c);
                return c;
                //while (_ticksWhenPolledKeyAppendedToGetLine == 0 || _polledTicks < _ticksWhenPolledKeyAppendedToGetLine)
                //{
                //    Thread.Sleep(25);
                //}
                //_ticksWhenPolledKeyAppendedToGetLine = _polledTicks;
                //return _polledKey;
            }

            var bytes = new byte[256];
            int i;
            char? result = null;
            session.Stream.Flush();
            if (session.ForceLogout)
                throw new ForceLogoutException(session.ForceLogoutReason);
            if (!session.ForceLogout && session.Stream.CanRead && session.Stream.CanWrite && (i = session.Stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                session.ResetIdleTimer();
                var data = GetString(bytes, 0, i);
                if (data?.Length >= 1)
                    result = data[0];
            }

            //if (_pollingOn && _polledKey != default && !_ticksWhenPolledKeyAppendedToGetLine.HasValue || _polledTicks > _ticksWhenPolledKeyAppendedToGetLine)
            //    _ticksWhenPolledKeyAppendedToGetLine = _polledTicks;

            return result;
        }

        protected string StreamInputLine(
            BbsSession session, 
            Func<string, string> autoComplete = null, 
            InputHandlingFlag handlingFlag = InputHandlingFlag.None)
        {
            session.LastReadMessageNumberWhenStartedTyping = null;

            void EndInput()
            {
                IsInputting = false;
                ShowDelayedNotifications();
            }

            try
            {
                session.Stream.Flush();

                var bytes = new byte[256];
                int i = 0;
                char? c = null;
                StringBuilder lineBuilder = new StringBuilder();

                while (
                    session != null &&
                    !session.IsDisposed &&
                    !session.ForceLogout &&
                    session.Stream.CanRead &&
                    session.Stream.CanWrite &&
                    (_pollingOn && TryGetKeyFromPoll(out c)) || (!_pollingOn && (i = session.Stream.Read(bytes, 0, bytes.Length)) != 0)
                    )
                {
                    session.ResetIdleTimer();

                    if (!session.LastReadMessageNumberWhenStartedTyping.HasValue)
                        session.LastReadMessageNumberWhenStartedTyping = session.LastReadMessageNumber;

                    IsInputting = lineBuilder.Length > 0;
                    if (c.HasValue && _pollingOn)
                    {
                        bytes = new[] { (byte)c.Value };
                        i = 1;
                    }

                    if (handlingFlag.HasFlag(InputHandlingFlag.AutoCompleteOnTab) &&
                        bytes?.Length > 0 &&
                        (bytes[0] == '\t' || bytes[0] == 17))
                    {
                        string acText = autoComplete?.Invoke(lineBuilder.ToString());
                        acText = TransformText(acText);
                        if (!string.IsNullOrWhiteSpace(acText))
                        {
                            bytes = GetBytes(acText);
                            i = acText.Length;
                        }
                    }

                    if (handlingFlag.HasFlag(InputHandlingFlag.InterceptSingleCharacterCommand) &&
                        lineBuilder.Length == 0 &&
                        Constants.LegitOneCharacterCommands.Contains((char)bytes[0]))
                    {
                        return new string((char)bytes[0], 1);
                    }

                    if (bytes[0] == 3)
                    {
                        // ctrl-c hit, abort inputting line
                        IsInputting = false;
                        lineBuilder.Clear();
                        using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                        {
                            session.Io.OutputLine($"{NewLine} *** ABORTED ***");
                            return null;
                        }
                    }

                    bytes = InterpretInput(bytes);

                    var isUpArrow =
                        (bytes.Length >= 3 && bytes[0] == 27 && bytes[1] == 91 && bytes[2] == 65) ||
                        (bytes.Length >= 1 && bytes[0] == 11);

                    isUpArrow |= 
                        !string.IsNullOrWhiteSpace(session.Io.Up) &&
                        bytes.SequenceEqual(session.Io.Up.Select(b => (byte)b));

                    var isDownArrow =
                        (bytes.Length >= 3 && bytes[0] == 27 && bytes[1] == 91 && bytes[2] == 66);

                    isDownArrow |=
                        !string.IsNullOrWhiteSpace(session.Io.Down) &&
                        bytes.SequenceEqual(session.Io.Down.Select(b => (byte)b));

                    if (handlingFlag.HasFlag(InputHandlingFlag.UseLastLine))
                    {
                        if (isUpArrow && _lastLineStack.Count > 0)
                        {
                            string lastLine;
                            do
                            {
                                lastLine = _lastLineStack.Pop();
                                _nextLineStack.Push(lastLine);
                                if (lastLine != lineBuilder.ToString())
                                    break;
                            } while (_lastLineStack.Count > 0);
                            if (lastLine != null)
                            {
                                bytes = GetBytes(lastLine);
                                i = lastLine.Length;
                                StreamOutput(session, new char[] { '\b', ' ', '\b' }.Repeat(lineBuilder.Length));
                                lineBuilder.Clear();
                            }
                        }
                        else if (isDownArrow && _nextLineStack.Count > 0)
                        {
                            string nextLine;
                            do
                            {
                                nextLine = _nextLineStack.Pop();
                                _lastLineStack.Push(nextLine);
                                if (nextLine != lineBuilder.ToString())
                                    break;
                            } while (_nextLineStack.Count > 0);
                            if (nextLine != null)
                            {
                                bytes = GetBytes(nextLine);
                                i = nextLine.Length;
                                StreamOutput(session, new char[] { '\b', ' ', '\b' }.Repeat(lineBuilder.Length));
                                lineBuilder.Clear();
                            }
                        }
                        else if (isUpArrow || isDownArrow)
                            continue;
                    }

                    RemoveInvalidInputCharacters(ref bytes);
                    if (bytes.Length < 1)
                        continue;
                    var data = GetString(bytes, 0, i);
                    
                    // fix for SyncTERM version 1.2b
                    if (data == "\r\0") data = NewLine; 

                    bool includedNewLine = false;

                    if (data == "\n")
                    {
                        if (handlingFlag.HasFlag(InputHandlingFlag.AllowCtrlEnterToAddNewLine))
                        {
                            data = NewLine;
                            includedNewLine = true;
                        }
                        else
                            continue;
                    }

                    // deal with one or more backspaces
                    foreach (var d in data)
                    {
                        if (d == '\b' || d == '\u007f')
                        {
                            // backspace
                            if (lineBuilder.Length > 0)
                            {
                                lineBuilder.Remove(lineBuilder.Length - 1, 1);
                                StreamOutput(session, '\b', ' ', '\b');
                                if (lineBuilder.Length == 0 || !lineBuilder.ToString().IsPrintable())
                                {
                                    lineBuilder.Clear();
                                    EndInput();
                                }
                            }
                            //continue;
                        }
                    }

                    // remove backspaces from data from this point on
                    data = data.Replace("\b", "").Replace("\u007f", "");
                    if (data.Length == 0)
                        continue; // only contained backspace(s)

                    if (handlingFlag.HasFlag(InputHandlingFlag.MaxLengthIfEmote) &&
                        lineBuilder.Length > Constants.MaxInputLength &&
                        lineBuilder.Length >= 3 &&
                        lineBuilder[0] == '/' &&
                        char.ToUpper(lineBuilder[1]) == 'M' &&
                        char.ToUpper(lineBuilder[2]) == 'E')
                    {
                        // don't append because we're limiting input length
                    }
                    else if (handlingFlag.HasFlag(InputHandlingFlag.MaxLength) &&
                        lineBuilder.Length > Constants.MaxInputLength)
                    {
                        // don't append because we're limiting input length
                    }
                    else
                    {
                        string echo = handlingFlag.HasFlag(InputHandlingFlag.PasswordInput) ? "*".Repeat(data.Length) : data;

                        if (includedNewLine || !handlingFlag.HasFlag(InputHandlingFlag.DoNotEchoNewlines) || echo != NewLine)
                            StreamOutput(session, echo, OutputHandlingFlag.NoWordWrap);

                        if (echo == "\r")
                            StreamOutput(session, "\n");

                        lineBuilder.Append(data);
                    }

                    if (lineBuilder.Length > 2 && lineBuilder.ToString().EndsWith("+++"))
                        return "/o"; // force logout

                    var returnNow =
                        (handlingFlag.HasFlag(InputHandlingFlag.ReturnFirstCharacterOnly) && lineBuilder.Length >= 1)
                        || (!includedNewLine && data.Contains("\r"));

                    returnNow |=
                        handlingFlag.HasFlag(InputHandlingFlag.ReturnFirstCharacterOnlyUnlessNumeric) &&
                        lineBuilder.Length >= 1 &&
                        !decimal.TryParse(lineBuilder.ToString(), out var _);

                    if (returnNow)
                    {
                        if (handlingFlag.HasFlag(InputHandlingFlag.PasswordInput))
                            StreamOutput(session, NewLine);
                        var result = lineBuilder.ToString()?.Replace("\0", "");
                        
                        // strip newlines from only the end of the result, leaving any that are in the middle or beginning.
                        while (true == result?.EndsWith("\r") || true == result?.EndsWith("\n"))
                            result = result.Substring(0, result.Length - 1);

                        if (handlingFlag.HasFlag(InputHandlingFlag.UseLastLine))
                        {
                            _lastLineStack.Push(result);
                            if (_nextLineStack.Count > 0 && _nextLineStack.Peek() == result)
                                _nextLineStack.Pop();
                        }
                        return result;
                    }

                }

                return null;
            } 
            finally
            {
                EndInput();
            }
        }

        public virtual void Flush()
        {
            _session.Stream.Flush();
        }

        #region KeyPolling
        public bool IsPolling => _pollingOn;
        private bool _pollingOn = false;
        private char? _polledKey;
        private long _polledTicks;
        private long? _ticksWhenPolledKeyAppendedToGetLine;
        private Thread _pollKeyThread = null;

        public virtual char? GetPolledKey()
        {
            var c = _polledKey;
            if (c.HasValue)
            {
                var txt = TransformText($"{c.Value}");
                return txt[0];
            }
            return _polledKey;
        }

        public bool IsPollingKeys => _pollKeyThread?.IsAlive == true;

        public void PollKey()
        {
            if (_pollKeyThread == null || !_pollKeyThread.IsAlive)
            {
                ThreadStart start = new ThreadStart(PollKeyInternal);
                _pollKeyThread = new Thread(start);
                _pollingOn = true;
                _pollKeyThread.Start();
            }
        }

        public void ClearPolledKey()
        {
            _polledKey = null;
        }

        public void AbortPollKey()
        {
            if (_pollKeyThread != null && _pollKeyThread.IsAlive)
            {
                _pollingOn = false;
                _pollKeyThread.Abort();
            }
        }

        public long GetPolledTicks()
        {
            return _polledTicks;
        }

        private void PollKeyInternal()
        {
            while (_pollingOn)
            {
                var bytes = new byte[256];
                int i;
                if (_session.ForceLogout || !_session.Stream.CanRead || !_session.Stream.CanWrite || !_session.IsConnected)
                    break;

                if ((i = _session.Stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    _session.ResetIdleTimer();
                    bytes = InterpretInput(bytes);
                    var data = GetString(bytes, 0, i);
                    if (data?.Length >= 1)
                    {
                        _polledKey = data[0];
                        _polledTicks = DateTime.Now.Ticks;
                    }
                }
            }
        }

        private bool TryGetKeyFromPoll(out char? c)
        {
            if (_pollingOn)
            {
                while (!_polledKey.HasValue || _ticksWhenPolledKeyAppendedToGetLine >= _polledTicks)
                {
                    Thread.Sleep(25);
                }
                _ticksWhenPolledKeyAppendedToGetLine = _polledTicks;
                c = _polledKey;
                return c.HasValue;
            }
            c = null;
            return false;
        }

        #endregion

        protected virtual void StartDelayedNotificationsTimer()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.Elapsed < Constants.DelayedNotificationsMaxWaitTime)
            {
                Thread.Sleep((int)(Constants.DelayedNotificationsMaxWaitTime.TotalMilliseconds / 4));
            }

            ShowDelayedNotifications();
        }

        protected virtual void ShowDelayedNotifications()
        {
            while (_delayedNotifications.Count > 0)
            {
                _delayedNotifications.Dequeue()?.Invoke();
            }
            if (_delayedNotificationThread != null && _delayedNotificationThread.IsAlive)
            {
                _delayedNotificationThread.Abort();
                _delayedNotificationThread = null;
            }
        }

        #endregion

        public virtual void SetUpper()
        {

        }

        public virtual void SetLower()
        {

        }
    }
}
