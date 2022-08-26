using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using miniBBS.UserIo;
using System;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class TermSetup
    {
        private const int _minRows = 5;
        private const int _minCols = 5;
        private const int _defaultRows = 24;
        private const int _defaultCols = 80;

        public static void Execute(BbsSession session, bool? cbmDetectedThroughDel = null)
        {
            var originalLocation = session.CurrentLocation;
            session.CurrentLocation = Module.ConfigureEmulation;
            
            try
            {
                session.Rows = session.User.Rows;
                session.Cols = session.User.Cols;
                
                var detEmu = session.Io.EmulationType;

                // this should only take effect on initial login, 
                // not when using /term command
                if (cbmDetectedThroughDel.HasValue)
                    detEmu = cbmDetectedThroughDel.Value ? TerminalEmulation.Cbm : TerminalEmulation.Ascii;

                if (session.User.TotalLogons == 1 && AskAnsi(session))
                {
                    detEmu = session.User.Emulation = TerminalEmulation.Ansi;
                }
                
                var lastEmu = session.User.Emulation;

                if (session.Rows < _minRows)
                    session.Rows = _defaultRows;
                if (session.Cols < _minCols)
                    session.Cols = _defaultCols;

                int lastCols, lastRows;
                lastCols = session.Cols;
                lastRows = session.Rows;

                var emulation = detEmu == TerminalEmulation.Cbm ? detEmu : lastEmu;

                var cols = lastCols;
                var rows = lastRows;

                Action ApplySettings = () =>
                {
                    switch (emulation)
                    {
                        case TerminalEmulation.Ascii: session.Io = new Ascii(session); break;
                        case TerminalEmulation.Ansi: session.Io = new ANSI(session); break;
                        case TerminalEmulation.Cbm: session.Io = new Cbm(session); break;
                    }
                    session.User.Rows = rows >= _minRows ? rows : session.Rows;
                    session.User.Cols = cols >= _minCols ? cols : session.Cols;
                    session.User.Emulation = emulation;
                    session.Cols = session.User.Cols;
                    session.Rows = session.User.Rows;
                };

                ApplySettings();

                while (true)
                {
                    session.Io.OutputLine($"{Environment.NewLine} -- Terminal Settings --");
                    session.Io.OutputLine($"(C)ols (width)  : {cols}");
                    session.Io.OutputLine($"(R)ows (height) : {rows}");
                    session.Io.OutputLine($"(E)mulation     : {emulation}");
                    session.Io.OutputLine(" --- Presets --- ");
                    session.Io.OutputLine("1) Try Auto-Detect");
                    session.Io.OutputLine($"2) Last    : {lastCols}c, {lastRows}r, {lastEmu}");
                    session.Io.OutputLine("3) 80c std : 80c, 24r");
                    session.Io.OutputLine("4) 40c std : 40c, 24r");
                    session.Io.OutputLine("5) Experiment");
                    session.Io.Output("Enter = Continue > ");
                    var chr = session.Io.InputKey();
                    session.Io.OutputLine();
                    switch (chr)
                    {
                        case 'r':
                        case 'R':
                            {
                                session.Io.Output($"Rows [{rows}] : ");
                                string s = session.Io.InputLine();
                                if (int.TryParse(s, out int r) && r > 5 && r < 255)
                                    rows = r;
                            }
                            ApplySettings();
                            break;
                        case 'c':
                        case 'C':
                            {
                                session.Io.Output($"Cols [{cols}] : ");
                                string s = session.Io.InputLine();
                                if (int.TryParse(s, out int c) && c > 10 && c < 255)
                                    cols = c;
                            }
                            ApplySettings();
                            break;
                        case 'e':
                        case 'E':
                            {
                                session.Io.Output(string.Format("{0}1 = ASCII{0}2 = ANSI{0}3 = PETSCII (CBM){0}Emulation [{1}] : ", Environment.NewLine, detEmu.ToString()));
                                char? s = session.Io.InputKey();
                                switch (s)
                                {
                                    case '1': emulation = TerminalEmulation.Ascii; break;
                                    case '2': emulation = TerminalEmulation.Ansi; break;
                                    case '3': emulation = TerminalEmulation.Cbm; break;
                                }
                            }
                            ApplySettings();
                            break;
                        case '1':
                            {
                                var detected = TryAutoDetectRowsAndCols(session);
                                if (detected.WasDetected)
                                {
                                    cols = detected.Cols;
                                    rows = detected.Rows;
                                    ApplySettings();
                                }
                            }
                            break;
                        case '2':
                            cols = lastCols;
                            rows = lastRows;
                            emulation = lastEmu;
                            ApplySettings();
                            break;
                        case '3':
                            cols = 80;
                            rows = 24;
                            ApplySettings();
                            break;
                        case '4':
                            cols = 40;
                            rows = 24;
                            ApplySettings();
                            break;
                        case '5':
                            {
                                var _exp = Experiment(session);
                                if (_exp.WasDetected)
                                {
                                    cols = _exp.Cols;
                                    rows = _exp.Rows;
                                    ApplySettings();
                                }
                            }
                            break;
                        default:
                            session.Io.OutputLine();
                            ApplySettings();
                            session.UserRepo.Update(session.User);
                            return;
                    }
                }
            }
            finally
            {
                session.CurrentLocation = originalLocation;
            }
        }

        private static bool AskAnsi(BbsSession session)
        {
            var colors = new[]
            {
                ConsoleColor.Red,
                ConsoleColor.Green,
                ConsoleColor.Yellow,
                ConsoleColor.Blue,
                ConsoleColor.Cyan,
                ConsoleColor.Magenta
            };

            const string msg = "Welcome to Mutiny Community!";

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
            {
                session.Io.OutputRaw(Encoding.ASCII.GetBytes($"Testing for ANSI support...{Environment.NewLine}"));
                ANSI ansi = new ANSI(session);
                int clr = 0;
                foreach (char c in msg)
                {
                    ansi.SetForeground(colors[clr++]);
                    if (clr >= colors.Length) clr = 0;
                    ansi.OutputRaw((byte)c);
                }
                ansi.SetForeground(ConsoleColor.White);
                session.Io.OutputRaw(Encoding.ASCII.GetBytes($"{Environment.NewLine}Is the above line in multiple colors?: "));
                var k = session.Io.InputKey();
                return k == 'y' || k == 'Y';
            }

            
        }

        private static TermSize TryAutoDetectRowsAndCols(BbsSession session)
        {
            var result = new TermSize();
            var willNegotiate = new byte[] { 255, 251, 31 };
            var negotiation = new byte[] { 255, 250, 31 };

            Func<byte[], bool> WillNegotiate = _b =>
                _b?.Length >= 3 &&
                _b[0] == willNegotiate[0] &&
                _b[1] == willNegotiate[1] &&
                _b[2] == willNegotiate[2];

            Func<byte[], bool> Negotiation = _b =>
                _b?.Length >= 3 &&
                _b[0] == negotiation[0] &&
                _b[1] == negotiation[1] &&
                _b[2] == negotiation[2];

            session.Io.OutputLine("Trying to auto-detect your terminal's rows & columns.  If this appears to hang just press enter/return.");

            session.Io.OutputRaw(255, 253, 31);
            var autoDetectBytes = session.Io.InputRaw();
            //session.Io.OutputLine($"Got: {string.Join(", ", autoDetectBytes.Select(x => (int)x))}");

            int? detCols, detRows;
            detCols = detRows = null;
            if (WillNegotiate(autoDetectBytes) && autoDetectBytes.Length < 12)
            {
                // only responded with 'will negotate' the actual negotation isn't in this packet
                // so fetch another packet
                autoDetectBytes = session.Io.InputRaw();
                //session.Io.OutputLine($"Got: {string.Join(", ", autoDetectBytes.Select(x => (int)x))}");
            }
            else if (autoDetectBytes.Length >= 12)
            {
                // got both the "will negotate" and the negotation in one packet, strip off the "will negotitate" bit
                autoDetectBytes = autoDetectBytes.Skip(3).ToArray();
            }

            if (Negotiation(autoDetectBytes) && autoDetectBytes.Length >= 9)
            {
                int offset = 3;
                detCols = (autoDetectBytes[offset] << 8) + autoDetectBytes[offset + 1];
                detRows = (autoDetectBytes[offset + 2] << 8) + autoDetectBytes[offset + 3];
            }
            session.Stream.Flush();

            result.Rows = detRows ?? default;
            result.Cols = detCols ?? default;

            return result;
        }

        private static TermSize Experiment(BbsSession session)
        {
            var result = new TermSize();

            session.Io.OutputLine("Okay first let's determine how many columns.  I'll let you type, just type a bunch of x's (or whatever character you want).  As soon as the cursor wraps around to the next line press enter/return.");
            session.Io.OutputLine("Start typing below:");
            var line = session.Io.InputLine();
            result.Cols = line.Length;

            session.Io.OutputLine($"Okay it looks like your terminal can display {result.Cols} columns.  Now let's work out how many rows can fit on one screen.");
            session.Io.OutputLine("Keep pressing '1' until the '<--' below is at the top of the screen.  As soon as it's there, press enter/return instead of '1'.");
            session.Io.Output("<--");
            int r = 1;
            bool keepGoing;
            do
            {
                var key = session.Io.InputKey();
                session.Io.OutputLine();
                if (key.HasValue && key.Value == '1')
                {
                    r++;
                    keepGoing = true;
                }
                else
                    keepGoing = false;
            } while (keepGoing);
            result.Rows = r;
            session.Io.OutputLine($"Okay it looks like your temrinal can display {result.Rows} rows.");

            return result;
        }

        private struct TermSize
        {
            public int Rows { get; set; }
            public int Cols { get; set; }
            public bool WasDetected => Rows >= _minRows && Cols >= _minCols;
        }
    }
}
