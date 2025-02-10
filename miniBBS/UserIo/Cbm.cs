using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.UserIo
{
    public class Cbm : UserIoBase
    {
        public override TerminalEmulation EmulationType => TerminalEmulation.Cbm;

        private static readonly IDictionary<byte, byte[]> _interpretationTable = new Dictionary<byte, byte[]>
        {
            {193, new byte[] { (byte)'a'} },
            {194, new byte[] { (byte)'b'} },
            {195, new byte[] { (byte)'c'} },
            {196, new byte[] { (byte)'d'} },
            {197, new byte[] { (byte)'e'} },
            {198, new byte[] { (byte)'f'} },
            {199, new byte[] { (byte)'g'} },
            {200, new byte[] { (byte)'h'} },
            {201, new byte[] { (byte)'i'} },
            {202, new byte[] { (byte)'j'} },
            {203, new byte[] { (byte)'k'} },
            {204, new byte[] { (byte)'l'} },
            {205, new byte[] { (byte)'m'} },
            {206, new byte[] { (byte)'n'} },
            {207, new byte[] { (byte)'o'} },
            {208, new byte[] { (byte)'p'} },
            {209, new byte[] { (byte)'q'} },
            {210, new byte[] { (byte)'r'} },
            {211, new byte[] { (byte)'s'} },
            {212, new byte[] { (byte)'t'} },
            {213, new byte[] { (byte)'u'} },
            {214, new byte[] { (byte)'v'} },
            {215, new byte[] { (byte)'w'} },
            {216, new byte[] { (byte)'x'} },
            {217, new byte[] { (byte)'y'} },
            {218, new byte[] { (byte)'z'} },
            {145, new byte[] { 27,91,65 } }, // up cursor
            {17, new byte[] { 27,91,66 } }, // down cursor
            {157, new byte[] { 27,91,68 } }, // left cursor
            //{29, new byte[] { 27,91,67 } }, // right cursor
            {29, new byte[] { 32 } }, // right cursor = space
            {19, new byte[] { 0} }, // home
            {147, new byte[] { 0} }, // clear
            {20, new byte[] { 8 } }, // replace DEL with backspace
            {148, new byte[] { 0} }, // insert
            {92, new byte[] { (byte)'£'} }, // british pound
            {94, new byte[] { (byte)'^'} }, // up arrow (not up cursor, the *arrow*),
            {141, new byte[] { 10 } }, // shift+enter = newline (ctrl+enter on PC)
            {221, new[] { (byte)'|'} },
        };

        //public override string NewLine => $"{(char)13}";

        const byte _revOn = 18;
        const byte _revOff = 146;
        const byte _toUpper = 142;
        const byte _toLower = 14;

        private static readonly IDictionary<byte, byte[]> _asciiToCbm = new Dictionary<byte, byte[]>
        {
            // 142 to uppercase, // 14 to lowercase
            // 18 rev on, // 146 rev off
            {3, new byte[] { 0x73 } }, // heart
            {4, new byte[] { 0x7a } }, // diamond
            {5, new byte[] { 0x78 } }, // club
            {6, new byte[] { 0x61 } }, // spade
            {16, new byte[] { 0x3c } }, // triangle pointing right (>)
            {17, new byte[] { 0x5f } }, // triangle pointing left
            {24, new byte[] { 0x5e } }, // up arrow
            {25, new byte[] { 0x56 } }, // down arrow (V)
            {27, new byte[] { 0x5f } }, // left arrow
            {26, new byte[] { 0x3c } }, // right arrow (>)
            {219, new byte[] { _revOn, 32, _revOff } }, // full block (inverse space)
            {220, new byte[] { 0xa2 } }, // lower block
            {221, new byte[] { 0xa1 } }, // left block
            {222, new byte[] { _revOn, 0xa1, _revOff } }, // right block (inverse left block)
            {223, new byte[] { _revOn, 0xa2, _revOff } }, // upper block (invserse lower block)
            {218, new byte[] { 0xb0 } }, // top/left border
            {196, new byte[] { 0xC3 } }, // horizontal line
            {191, new byte[] { 0xae } }, // top/right border
            {179, new byte[] { 0xC2 } }, // vertical line
            {192, new byte[] { 0xad } }, // bottom/left border
            {217, new byte[] { 0xbd } }, // bottom/right border
            {195, new byte[] { 0xab } }, // left 'T'
            {180, new byte[] { 0xb3 } }, // right 'T'
            {194, new byte[] { 0xb2 } }, // top 'T'
            {193, new byte[] { 0xb1 } }, // bottom 'T'
            {197, new byte[] { 0x7b } }, // '+' bordering char
        };

        public Cbm(BbsSession session)
            : base(session)
        {

        }

        public override string Bold => string.Empty;

        public override string Underline => string.Empty;

        public override string Reversed => $"{(char)18}";

        public override string Up => $"{(char)145}";

        public override string Down => $"{(char)17}";

        public override string Left => $"{(char)157}";

        public override string Right => $"{(char)29}";

        public override string Home => $"{(char)19}";

        public override string Clear => $"{(char)147}";

        public override void ClearLine()
        {
            
        }

        public override void ClearScreen()
        {
            OutputRaw(new[] { (byte)147 });
        }

        public override void SetPosition(int x, int y)
        {
            
        }

        protected override string GetBackgroundString(ConsoleColor color)
        {
            return string.Empty;
        }

        protected override string GetForegroundString(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Black: return $"{(char)144}";
                case ConsoleColor.White: return $"{(char)5}";
                case ConsoleColor.Red: return $"{(char)150}";
                case ConsoleColor.Cyan: return $"{(char)159}";
                case ConsoleColor.Magenta: return $"{(char)156}";
                case ConsoleColor.Green: return $"{(char)153}";
                case ConsoleColor.Blue: return $"{(char)154}";
                case ConsoleColor.Yellow: return $"{(char)158}";
                case ConsoleColor.DarkYellow: return $"{(char)149}";
                case ConsoleColor.DarkRed: return $"{(char)28}";
                case ConsoleColor.DarkGray: return $"{(char)151}";
                case ConsoleColor.Gray: return $"{(char)152}";
                case ConsoleColor.DarkGreen: return $"{(char)30}";
                case ConsoleColor.DarkBlue: return $"{(char)31}";
                default: return string.Empty;
            }
        }

        public override void SetForeground(ConsoleColor color)
        {
            if (_currentForeground != color)
            {
                _currentForeground = color;
                byte b = 0;
                switch (color)
                {
                    case ConsoleColor.Black: b = 144; break;
                    case ConsoleColor.White: b = 5; break;
                    case ConsoleColor.Red: b = 150; break;
                    case ConsoleColor.Cyan: b = 159; break;
                    case ConsoleColor.Magenta: b = 156; break;
                    case ConsoleColor.Green: b = 153; break;
                    case ConsoleColor.Blue: b = 154; break;
                    case ConsoleColor.Yellow: b = 158; break;
                    case ConsoleColor.DarkYellow: b = 149; break;
                    case ConsoleColor.DarkRed: b = 28; break;
                    case ConsoleColor.DarkGray: b = 151; break;
                    case ConsoleColor.Gray: b = 152; break;
                    case ConsoleColor.DarkGreen: b = 30; break;
                    case ConsoleColor.DarkBlue: b = 31; break;
                }
                OutputRaw(new[] { b });
            }
        }

        private readonly byte[] _colorBytes = new byte[]
        {
            144, 31, 30, 159, 28, 156, 158, 152, 151, 154, 153, 159, 150, 156, 158, 5
        };

        public override void Output(string s, OutputHandlingFlag flags = OutputHandlingFlag.None)
        {
            base.Output(TransformText(s), flags);
        }

        public override void Output(char c)
        {
            c = char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c);
            base.Output(c);
        }

        public override void OutputLine(string s = null, OutputHandlingFlag flags = OutputHandlingFlag.None)
        {
            base.OutputLine(TransformText(s), flags);
        }

        public override void OutputBackspace()
        {
            base.Output((char)8);
        }

        //public override char? GetPolledKey()
        //{
        //    var c = base.GetPolledKey();
        //    if (c.HasValue && c >= 'A' - 1 && c <= 'Z' - 1)
        //        c = (char)(c+2);
        //    //if (c.HasValue)
        //    //    c = char.IsUpper(c.Value) ? char.ToLower(c.Value) : char.ToUpper(c.Value);
        //    return c;
        //}

        protected override string GetString(byte[] bytes) =>
            new string(bytes.Select(c => (char)c).ToArray());

        protected override string GetString(byte[] bytes, int index, int count)
        {
            var arr = bytes.Select(c => (char)c).ToArray();
            return new string(arr, index, count);
        }

        public override char? InputKey()
        {
            var c = base.InputKey();
            if (c.HasValue)
                c = char.IsUpper(c.Value) ? char.ToLower(c.Value) : char.ToUpper(c.Value);
            return c;
        }

        public override string InputLine(InputHandlingFlag handlingFlag = InputHandlingFlag.None)
        {
            return TransformText(base.InputLine(handlingFlag));
        }

        /// <summary>
        /// Does any kind of special transforms needed for this emulation type
        /// </summary>
        public override string TransformText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var chrs = new List<char>();

            for (int i=0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == Constants.Inverser)
                    continue;
                if (_asciiToCbm.TryGetValue((byte)c, out var b))
                    chrs.AddRange(b.Select(x => (char)x));
                else if (c == 8)
                    chrs.Add((char)20); // backspace
                else if (char.IsUpper(c))
                    chrs.Add(char.ToLower(c));
                else
                    chrs.Add(char.ToUpper(c));
            }

            var arr = chrs.ToArray();
            text = new string(arr, 0, arr.Length);

            return text;
        }

        protected override byte[] InterpretInput(byte[] arr)
        {
            List<byte> byteList = new List<byte>();

            for (int i=0; i < arr.Length; i++)
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
            if (text == "\b")
            {
                // backspace (replace with left, space, left)
                base.StreamOutput(session, (char)157, ' ', (char)157);
            }
            else
                base.StreamOutput(session, text, flags);
        }

        protected override void StreamOutput(BbsSession session, params char[] characters)
        {
            for (int i=0; i < characters.Length; i++)
            {
                if (characters[i] == '\b')
                    characters[i] = (char)157;
            }

            base.StreamOutput(session, characters);
        }

        protected override void RemoveInvalidInputCharacters(ref byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                if (b == 13 || b == 10 || b == 8 || b == 20)
                    continue;
                if (b >= 193 && b <= 218)
                    continue; // upper case petscii
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

        protected override string ReplaceInlineColors(string line, out int actualTextLength)
        {
            // for every 2 control chars, 3 bytes need to be removed from length
            actualTextLength = line?.Length ?? 0;
            var ctrls = line?.Count(c => c == Constants.InlineColorizer) ?? 0;
            ctrls /= 2;
            actualTextLength -= ctrls * 3;

            return line;
        }

        public override byte[] GetBytes(string text)
        {
            byte[] bytes = text.Select(b => (byte)b).ToArray();
            
            // look for and replace inline color codes
            int p = text.IndexOf(Constants.InlineColorizer);
            while (p >= 0)
            {
                int end = text.IndexOf(Constants.InlineColorizer, p + 1);
                if (end == -1)
                {
                    // no ending inline colorizer found, assume end of string
                    end = text.Length - 1;
                }
                if (end > p)
                {
                    p++;
                    string s = text.Substring(p, end - p);
                    if (int.TryParse(s, out int clr))
                    {
                        if (clr < 0)
                            bytes[p - 1] = _colorBytes[(int)GetForeground()];
                        else
                            bytes[p - 1] = _colorBytes[clr];
                        for (int i = p; i <= end; i++)
                            bytes[i] = 0;
                    }
                }
                p = text.IndexOf(Constants.InlineColorizer, end+1);
            }

            bytes = bytes
                .Where(b => b > 0)
                .ToArray();

            return bytes;
        }

        public override void SetUpper()
        {
            _session.Io.OutputRaw(_toUpper);
        }

        public override void SetLower()
        {
            _session.Io.OutputRaw(_toLower);
        }
    }
}
