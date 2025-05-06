using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace miniBBS.UserIo
{
    public class ANSI : UserIoBase
    {
        public ANSI(BbsSession session)
            : base(session)
        {

        }

        public override TerminalEmulation EmulationType => TerminalEmulation.Ansi;
        public override string Backspace => "\b";

        private const string _deviceStatusRequest = "\u001b[6n";

        public override void ClearLine() { Output("\u001b[0K"); }
        public override void ClearScreen() { Output(Clear); }

        private const string Reset = "\u001b[0m";

        public override string Bold => "\u001b[1m";

        public override string Underline => "\u001b[4m";

        public override string Reversed => "\u001b[7m";

        public override string NotReversed => "\u001b[27m";

        public override string Up => "\u001b[1A";

        public override string Down => "\u001b[1B";

        public override string Left => "\u001b[1D";

        public override string Right => "\u001b[1C";

        public override string Home => "\u001b[1;1H";

        public override string Clear => "\u001b[2J";
                
        public override void SetPosition(int x, int y)
        {
            Output($"\u001b[{y};{x}H");
        }

        protected override string GetBackgroundString(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Black: return "\u001b[0;40m";
                case ConsoleColor.DarkRed: return "\u001b[0;41m";
                case ConsoleColor.DarkGreen: return "\u001b[0;42m";
                case ConsoleColor.DarkYellow: return "\u001b[0;43m";
                case ConsoleColor.DarkBlue: return "\u001b[0;44m";
                case ConsoleColor.DarkMagenta: return "\u001b[0;45m";
                case ConsoleColor.DarkCyan: return "\u001b[0;46m";
                case ConsoleColor.Gray: return "\u001b[0;47m";
                case ConsoleColor.DarkGray: return "\u001b[1;40m";
                case ConsoleColor.Red: return "\u001b[1;41m";
                case ConsoleColor.Green: return "\u001b[1;42m";
                case ConsoleColor.Yellow: return "\u001b[1;43m";
                case ConsoleColor.Blue: return "\u001b[1;44m";
                case ConsoleColor.Magenta: return "\u001b[1;45m";
                case ConsoleColor.Cyan: return "\u001b[1;46m";
                case ConsoleColor.White: return "\u001b[1;47m";
                default: return string.Empty;
            }
        }

        protected override string GetForegroundString(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Black: return "\u001b[0;30m";
                case ConsoleColor.DarkRed: return "\u001b[0;31m";
                case ConsoleColor.DarkGreen: return "\u001b[0;32m";
                case ConsoleColor.DarkYellow: return "\u001b[0;33m";
                case ConsoleColor.DarkBlue: return "\u001b[0;34m";
                case ConsoleColor.DarkMagenta: return "\u001b[0;35m";
                case ConsoleColor.DarkCyan: return "\u001b[0;36m";
                case ConsoleColor.DarkGray: return "\u001b[0;37m";
                case ConsoleColor.Gray: return "\u001b[1;37m";
                case ConsoleColor.Red: return "\u001b[1;31m";
                case ConsoleColor.Green: return "\u001b[1;32m";
                case ConsoleColor.Yellow: return "\u001b[1;33m";
                case ConsoleColor.Blue: return "\u001b[1;34m";
                case ConsoleColor.Magenta: return "\u001b[1;35m";
                case ConsoleColor.Cyan: return "\u001b[1;36m";
                case ConsoleColor.White: return "\u001b[1;37m";
                default: return string.Empty;
            }
        }

        protected override string ReplaceInlineColors(string line, out int actualTextLength)
        {
            actualTextLength = 0;
            var resultBuilder = new StringBuilder();
            var codeBuilder = new StringBuilder();
            bool inCode = false;

            foreach (var c in line)
            {
                if (c == Constants.Inverser)
                    continue;
                if (c == Constants.InlineColorizer)
                {
                    inCode = !inCode;
                    if (!inCode && int.TryParse(codeBuilder.ToString(), out int code) && code >= -1 && code <= 15)
                    {
                        var clr = code == -1 ? _currentForeground : (ConsoleColor)code;
                        resultBuilder.Append(GetForegroundString(clr));
                    }
                    codeBuilder.Clear();
                }
                else if (inCode)
                    codeBuilder.Append(c);
                else
                {
                    resultBuilder.Append(c);
                    actualTextLength++;
                }
            }

            return resultBuilder.ToString();
        }

        public static bool TryAutoDetect(BbsSession session)
        {
            var sw = new Stopwatch();
            
            session.Io.OutputLine(_deviceStatusRequest);
            session.Io.Output("Does your terminal support ANSI color? (Y)es, (N)o or don't know: ");

            sw.Start();
            while (sw.ElapsedMilliseconds < 500)
            {
                Thread.Sleep(25);
            }
            var response = session.Io.InputRaw();
            session.Io.OutputLine("\u001b[2K"); // clear line
            session.Io.Output("\u001b[1A"); // up

            if (response == null || response.Length < 1)
                return false;
            if (response[0] == 27)
            {
                session.Io.OutputLine("ANSI Auto-Detected!");
                return true;
            }

            return response[0] == 'y' || response[0] == 'Y';
        }

        //public override void Output(string s, OutputHandlingFlag flags = OutputHandlingFlag.None)
        //{
        //    base.Output(TransformText(s), flags);
        //}

        //public override void OutputLine(string s = null, OutputHandlingFlag flags = OutputHandlingFlag.None)
        //{
        //    base.OutputLine(TransformText(s), flags);
        //}

        //public override string TransformText(string text)
        //{
        //    if (string.IsNullOrWhiteSpace(text) || !text.Any(c => c == Constants.Inverser))
        //        return base.TransformText(text);

        //    var isReversed = false;
        //    ConsoleColor fg = GetForeground();
        //    ConsoleColor bg = GetBackground();
        //    var builder = new StringBuilder();
        //    foreach (var c in text)
        //    {
        //        if (c != Constants.Inverser)
        //        {
        //            builder.Append(c);
        //            continue;
        //        }
        //        isReversed = !isReversed;
        //        if (isReversed)
        //            builder.Append(Reversed);
        //        else
        //            builder.Append(NotReversed);
        //    }

        //    if (isReversed)
        //        builder.Append(NotReversed);

        //    text = builder.ToString();
        //    return text;
        //}
    }
}
