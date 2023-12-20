using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using System;
using System.Text;

namespace miniBBS.UserIo
{
    public class ANSI : UserIoBase
    {
        public ANSI(BbsSession session)
            : base(session)
        {

        }

        public override TerminalEmulation EmulationType => TerminalEmulation.Ansi;

        public override void ClearLine() { Output("\u001b[0K"); }
        public override void ClearScreen() { Output(Clear); }

        public override string Bold => "\u001b[1m";

        public override string Underline => "\u001b[4m";

        public override string Reversed => "\u001b[7m";

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
                case ConsoleColor.Black: return "\u001b[40m";
                case ConsoleColor.DarkRed: return "\u001b[41m";
                case ConsoleColor.DarkGreen: return "\u001b[42m";
                case ConsoleColor.DarkYellow: return "\u001b[43m";
                case ConsoleColor.DarkBlue: return "\u001b[44m";
                case ConsoleColor.DarkMagenta: return "\u001b[45m";
                case ConsoleColor.DarkCyan: return "\u001b[46m";
                case ConsoleColor.Gray: return "\u001b[47m";
                case ConsoleColor.DarkGray: return "\u001b[40;1m";
                case ConsoleColor.Red: return "\u001b[41;1m";
                case ConsoleColor.Green: return "\u001b[42;1m";
                case ConsoleColor.Yellow: return "\u001b[43;1m";
                case ConsoleColor.Blue: return "\u001b[44;1m";
                case ConsoleColor.Magenta: return "\u001b[45;1m";
                case ConsoleColor.Cyan: return "\u001b[46;1m";
                case ConsoleColor.White: return "\u001b[47;1m";
                default: return string.Empty;
            }
        }

        protected override string GetForegroundString(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Black: return "\u001b[30m";
                case ConsoleColor.DarkRed: return "\u001b[31m";
                case ConsoleColor.DarkGreen: return "\u001b[32m";
                case ConsoleColor.DarkYellow: return "\u001b[33m";
                case ConsoleColor.DarkBlue: return "\u001b[34m";
                case ConsoleColor.DarkMagenta: return "\u001b[35m";
                case ConsoleColor.DarkCyan: return "\u001b[36m";
                case ConsoleColor.Gray: return "\u001b[37m";
                case ConsoleColor.DarkGray: return "\u001b[30;1m";
                case ConsoleColor.Red: return "\u001b[31;1m";
                case ConsoleColor.Green: return "\u001b[32;1m";
                case ConsoleColor.Yellow: return "\u001b[33;1m";
                case ConsoleColor.Blue: return "\u001b[34;1m";
                case ConsoleColor.Magenta: return "\u001b[35;1m";
                case ConsoleColor.Cyan: return "\u001b[36;1m";
                case ConsoleColor.White: return "\u001b[37;1m";
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
    }

}
