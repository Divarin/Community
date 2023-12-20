using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.UserIo
{
    public class Atascii : UserIoBase
    {
        public override TerminalEmulation EmulationType => TerminalEmulation.Atascii;
        
        /// <summary>
        /// From Atascii to Ascii
        /// </summary>
        private static readonly IDictionary<byte, byte[]> _interpretationTable = new Dictionary<byte, byte[]>
        {
            {155, new byte[] { 13 } }, // enter
            {28, new byte[] { 27,91,65 } }, // up cursor
            {29, new byte[] { 27,91,66 } }, // down cursor
            {30, new byte[] { 27,91,68 } }, // left cursor
            //{29, new byte[] { 27,91,67 } }, // right cursor
            {31, new byte[] { 32 } }, // right cursor = space
            {19, new byte[] { 0 } }, // home
            {125, new byte[] { 0 } }, // clear
            {254, new byte[] { 127 } }, // replace DEL with backspace
            {126, new byte[] { 127 } }, // backspace
            {127, new byte[] { 9 } }, // tab
            {255, new byte[] { 27, 91, 50, 126 } }, // insert            
            {124, new[] { (byte)'|'} },
        };

        private static readonly IDictionary<byte, byte> _asciiToAtascii = new Dictionary<byte, byte>
        {
            {3, 0 }, // heart
            {4, 96 }, // diamond
            {5, 16 }, // club
            {6, 123 }, // spade
            {16, 0x7f }, // triangle pointing right
            {17, 0x7e }, // triangle pointing left
            {24, 0x1c }, // up arrow
            {25, 0x1d }, // down arrow
            {27, 0x1e }, // left arrow
            {26, 0x1f }, // right arrow
            {219, 0xa0 }, // full block (inverse space)
            {220, 0x0e }, // lower block
            {221, 0x16 }, // left block
            {222, 0x99 }, // right block (inverse left block)
            {223, 0x0d }, // upper block
            {218, 0x11 }, // top/left border
            {196, 0x12 }, // horizontal line
            {191, 0x05 }, // top/right border
            {179, 0x7c }, // vertical line
            {192, 0x1a }, // bottom/left border
            {217, 0x03 }, // bottom/right border
            {195, 0x01 }, // left 'T'
            {180, 0x04 }, // right 'T'
            {194, 0x17 }, // top 'T'
            {193, 0x18 }, // bottom 'T'
            {197, 0x13 }, // '+' bordering char
        };

        public override string NewLine => $"{(char)155}";

        public Atascii(BbsSession session)
            : base(session)
        {

        }

        public override string Bold => string.Empty;

        public override string Underline => string.Empty;

        public override string Reversed => $"{(char)18}";

        public override string Up => $"{(char)28}";

        public override string Down => $"{(char)29}";

        public override string Left => $"{(char)30}";

        public override string Right => $"{(char)31}";

        public override string Home => $"{(char)19}";

        public override string Clear => $"{(char)125}";

        public override void SetPosition(int x, int y)
        {
            Output(string.Empty);
        }

        public override void ClearLine() { Output(string.Empty); }
        public override void ClearScreen() { Output(Clear); }

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

            bool inInverseMode = false;
            bool inColorCode = false;

            var chrs = new List<char>();
            foreach (var c in line)
            {
                if (c == Constants.InlineColorizer)
                    inColorCode = !inColorCode;
                else if (c == Constants.Inverser)
                    inInverseMode = !inInverseMode;
                else if (!inColorCode)
                {
                    if (inInverseMode && c <= 128)
                        chrs.Add((char)(c + 128));
                    else
                        chrs.Add(c);
                    actualTextLength++;
                }
            }

            return new string(chrs.ToArray());
        }

        protected override byte[] InterpretInput(byte[] arr)
        {
            List<byte> byteList = new List<byte>();

            for (int i = 0; i < arr.Length; i++)
            {
                var b = arr[i];
                if (_interpretationTable.ContainsKey(b))
                {
                    foreach (byte by in _interpretationTable[b])
                        byteList.Add(by);
                }
                else
                    byteList.Add(b);
            }

            return byteList.ToArray();
        }

        protected override void StreamOutput(BbsSession session, string text, OutputHandlingFlag flags = OutputHandlingFlag.None)
        {
            text = text
                .Replace('\r', NewLine[0])
                .Replace("\n", "");

            if (text == "\b")
            {
                // backspace (replace with left, space, left)
                base.StreamOutput(session, (char)30, ' ', (char)30);
            }
            else
                base.StreamOutput(session, text, flags);
        }

        protected override void StreamOutput(BbsSession session, params char[] characters)
        {
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i] == '\b')
                    characters[i] = (char)30;
            }

            base.StreamOutput(session, characters);
        }

        /// <summary>
        /// Does any kind of special transforms needed for this emulation type
        /// </summary>
        public override string TransformText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var result = new List<char>();

            for (int i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case (char)13: result.Add((char)155); break; // newline
                    case (char)10: break; // skip linefeeds
                    case '\b': result.Add((char)126); break; // backspace
                    default:
                        if (_asciiToAtascii.TryGetValue((byte)text[i], out var atascii))
                            result.Add((char)atascii);
                        else
                            result.Add(text[i]); 
                        break;
                }
            }

            var arr = result.ToArray();
            text = new string(arr, 0, arr.Length);
            return text;
        }

        public override void Output(string s, OutputHandlingFlag flags = OutputHandlingFlag.None)
        {
            base.Output(TransformText(s), flags);
        }

        public override void OutputLine(string s = null, OutputHandlingFlag flags = OutputHandlingFlag.None)
        {
            base.OutputLine(TransformText(s), flags);
        }

        public override void OutputBackspace()
        {
            base.Output((char)126);
        }

        public override string InputLine(InputHandlingFlag handlingFlag = InputHandlingFlag.None)
        {
            return TransformText(base.InputLine(handlingFlag));
        }

        public override byte[] GetBytes(string text)
        {
            return text.Select(b => (byte)b).ToArray();
        }

        protected override string GetString(byte[] bytes) => new string(bytes.Select(b => (char)b).ToArray());

        protected override string GetString(byte[] bytes, int index, int count)
        {
            var chars = bytes.Select(b => (char)b).ToArray();
            return new string(chars, index, count);
        }

    }
}
