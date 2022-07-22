using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using miniBBS.Interfaces;
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

        public virtual string InputLine(char? echoChar = null)
        {
            return StreamInputLine(_session, echoChar);
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
                Output(ansi);
            }
        }

        public virtual void SetBackground(ConsoleColor color)
        {
            if (_currentBackground != color)
            {
                string ansi = GetBackgroundString(color);
                _currentBackground = color;
                Output(ansi);
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
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.DarkRed))
            {
                //                                   1234567890123456789012345678901234567890
                string more = Environment.NewLine + "[more? (y,n,c) or page (u)p]";
                if (percentage.HasValue)
                    more += $" {Math.Round(100 * percentage.Value, 0)}% ";

                if (session.Io.EmulationType == TerminalEmulation.Cbm)
                    more = more.ToUpper();

                var r = Encoding.ASCII.GetBytes(more);
                
                session.Stream.Write(r, 0, r.Length);
                var k = StreamInput(session);
                if (k != '/')
                {
                    r[0] = 13;
                    r[1] = 10;
                    session.Stream.Write(r, 0, 2);
                }

                if (k == 'c' || k == 'C')
                    return PauseResult.Continuous;
                else if (k == 'n' || k == 'N')
                    return PauseResult.No;
                else if (k == 'u' || k == 'U' || k == 27 || k == 'p' || k == 'P')
                    return PauseResult.PageUp;
                else if (k == '/')
                {
                    StreamOutput(session, "Search: ");
                    string search = StreamInputLine(session);
                    if (!string.IsNullOrWhiteSpace(search))
                        keywordSearch.Keyword = search;
                    keywordSearch.From = SearchFrom.AfterPreviousMatch_WithoutWraparound;
                    return PauseResult.ExecuteKeywordSearch;
                }
            }
            return PauseResult.Yes;
        }

        protected virtual byte[] InterpretInput(byte[] arr)
        {
            return arr;
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
            text = ReplaceInlineColors(text);


            if (session.Stream.CanWrite)
            {
                if (text.Length > session.Cols)
                {
                    // handle wordwrap and pause
                    var lines = text.SplitAndWrap(session, flags).ToList();
                    if (flags.HasFlag(OutputHandlingFlag.PauseAtEnd))
                        lines.Add(" --- End of Document ---");
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

                        if (!continuousOutput && (row >= session.Rows-3) || (l >= lines.Count && flags.HasFlag(OutputHandlingFlag.PauseAtEnd)))
                        {
                            var pcent = (double)l / totalLines;
                            var pauseResult = Pause(session, keywordSearch, pcent);
                            if (pauseResult == PauseResult.No)
                                break;
                            else if (pauseResult == PauseResult.Continuous)
                                continuousOutput = true;
                            else if (pauseResult == PauseResult.PageUp)
                            {
                                l = DoPageUp(l);
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
                                        StreamOutputLine(session, $"No more occurances of '{keywordSearch.Keyword}' found.");
                                    }
                                    l = DoPageUp(l);
                                    continue;
                                }
                            }
                            pageMarkers.Push(l);
                            row = 0;
                        }

                        if (line != null)
                        {
                            var r = GetBytes(line);
                            session.Stream.Write(r, 0, r.Length);
                        }
                    }
                }
                else 
                {
                    var r = GetBytes(text);
                    session.Stream.Write(r, 0, r.Length);
                }
            }
        }

        protected abstract string ReplaceInlineColors(string line);
        protected virtual byte[] GetBytes(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }

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
            StreamOutput(session, Environment.NewLine, flags);
        }

        protected virtual void StreamOutputLine(BbsSession session, string text, OutputHandlingFlag flags = OutputHandlingFlag.None)
        {
            StreamOutput(session, text:$"{text}{Environment.NewLine}", flags);
        }

        protected virtual char? StreamInput(BbsSession session)
        {
            var bytes = new byte[256];
            int i;

            session.Stream.Flush();
            if (!session.ForceLogout && session.Stream.CanRead && session.Stream.CanWrite && (i = session.Stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                session.ResetIdleTimer();
                var data = Encoding.ASCII.GetString(bytes, 0, i);
                if (data?.Length >= 1)
                    return data[0];
            }
            return null;
        }

        protected string StreamInputLine(BbsSession session, char? echoChar = null)
        {
            Action EndInput = () =>
            {
                IsInputting = false;
                ShowDelayedNotifications();
            };

            try
            {
                session.Stream.Flush();

                var bytes = new byte[256];
                int i;
                StringBuilder lineBuilder = new StringBuilder();

                while (!session.ForceLogout && session.Stream.CanRead && session.Stream.CanWrite && (i = session.Stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    session.ResetIdleTimer();

                    IsInputting = lineBuilder.Length > 0;

                    if (bytes[0] == 3)
                    {
                        // ctrl-c hit, abort inputting line
                        IsInputting = false;
                        lineBuilder.Clear();
                        using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                        {
                            session.Io.OutputLine($"{Environment.NewLine} *** ABORTED ***");
                            return null;
                        }
                    }

                    RemoveInvalidInputCharacters(ref bytes);
                    if (bytes.Length < 1)
                        continue;

                    var data = Encoding.ASCII.GetString(InterpretInput(bytes), 0, i);

                    string echo = echoChar.HasValue ? echoChar.Value.Repeat(data.Length) : data;

                    if (data == "\b" || data == "\u007f")
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
                        continue;
                    }

                    StreamOutput(session, echo);
                    if (echo == "\r")
                        StreamOutput(session, "\n");

                    lineBuilder.Append(data);

                    if (lineBuilder.Length > 2 && lineBuilder.ToString().EndsWith("+++"))
                        return "/o"; // force logout

                    if (data.Contains("\r"))
                    {
                        if (echoChar.HasValue)
                            StreamOutput(session, Environment.NewLine);
                        return lineBuilder.ToString()?.Replace("\r", "")?.Replace("\n", "")?.Replace("\0", "");
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

        //private class StreamOutputControlFlag
        //{
        //    public bool UserAborted { get; set; } = false;
        //    public double ReadBytes { get; set; }
        //    public double TotalBytes { get; set; }
        //}
    }
}
