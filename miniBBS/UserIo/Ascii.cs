using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.UserIo
{
    public class Ascii : UserIoBase
    {
        public Ascii(BbsSession session)
            : base(session)
        {

        }

        public override TerminalEmulation EmulationType => TerminalEmulation.Ascii;

        public override void ClearLine() { Output(string.Empty); }
        public override void ClearScreen() { Output(Clear); }

        public override string Bold => string.Empty;

        public override string Underline => string.Empty;

        public override string Reversed => string.Empty;

        public override string Up => string.Empty;

        public override string Down => string.Empty;

        public override string Left => string.Empty;

        public override string Right => string.Empty;

        public override string Home => string.Empty;

        public override string Clear => string.Empty;

        public override void SetPosition(int x, int y)
        {
            Output(string.Empty);
        }

        protected override string GetBackgroundString(ConsoleColor color)
        {
            return string.Empty;
        }

        protected override string GetForegroundString(ConsoleColor color)
        {
            return string.Empty;
        }

        protected override string ReplaceInlineColors(string line, out int actualTextLength)
        {
            actualTextLength = 0;

            if (string.IsNullOrWhiteSpace(line))
                return line;

            bool inColorCode = false;
            var chrs = new List<char>();
            foreach (var c in line)
            {
                if (c == Constants.InlineColorizer)
                    inColorCode = !inColorCode;
                else if (c != Constants.Inverser && !inColorCode)
                {
                    chrs.Add(c);
                    actualTextLength++;
                }
            }

            return new string(chrs.ToArray());
        }

    }
}
