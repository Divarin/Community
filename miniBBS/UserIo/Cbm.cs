using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            {29, new byte[] { 27,91,67 } }, // right cursor
            {19, new byte[] { 0} }, // home
            {147, new byte[] { 0} }, // clear
            {20, new byte[] { 8 } }, // replace DEL with backspace
            {148, new byte[] { 0} }, // insert
            {92, new byte[] { (byte)'£'} }, // british pound
            {94, new byte[] { (byte)'^'} }, // up arrow (not up cursor, the *arrow*)
        };

        public Cbm(BbsSession session)
            : base(session)
        {

        }

        public override string Bold => string.Empty;

        public override string Underline => string.Empty;

        public override string Reversed => $"{(char)18}";

        public override string Up => string.Empty;

        public override string Down => string.Empty;

        public override string Left => string.Empty;

        public override string Right => string.Empty;

        public override string Home => string.Empty;

        public override string Clear => string.Empty;

        public override void ClearLine()
        {
            
        }

        public override void ClearScreen()
        {
            
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
                switch (color)
                {
                    case ConsoleColor.Black: StreamOutput(_session, (char)144); break;
                    case ConsoleColor.White: StreamOutput(_session, (char)5); break;
                    case ConsoleColor.Red: StreamOutput(_session, (char)150); break;
                    case ConsoleColor.Cyan: StreamOutput(_session, (char)159); break;
                    case ConsoleColor.Magenta: StreamOutput(_session, (char)156); break;
                    case ConsoleColor.Green: StreamOutput(_session, (char)153); break;
                    case ConsoleColor.Blue: StreamOutput(_session, (char)154); break;
                    case ConsoleColor.Yellow: StreamOutput(_session, (char)158); break;
                    case ConsoleColor.DarkYellow: StreamOutput(_session, (char)149); break;
                    case ConsoleColor.DarkRed: StreamOutput(_session, (char)28); break;
                    case ConsoleColor.DarkGray: StreamOutput(_session, (char)151); break;
                    case ConsoleColor.Gray: StreamOutput(_session, (char)152); break;
                    case ConsoleColor.DarkGreen: StreamOutput(_session, (char)30); break;
                    case ConsoleColor.DarkBlue: StreamOutput(_session, (char)31); break;
                }
            }
        }

        private byte[] _colorBytes = new byte[]
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
        protected override string TransformText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            char[] arr = new char[text.Length];

            for (int i=0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsUpper(c))
                    arr[i] = char.ToLower(c);
                else
                    arr[i] = char.ToUpper(c);
            }

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

        protected override byte[] GetBytes(string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            
            // look for and replace inline color codes
            int p = text.IndexOf(Constants.InlineColorizer);
            while (p >= 0)
            {
                int end = text.IndexOf(Constants.InlineColorizer, p + 1);
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
    }
}
