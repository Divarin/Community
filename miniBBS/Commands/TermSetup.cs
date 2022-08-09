using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.UserIo;
using System;

namespace miniBBS.Commands
{
    public static class TermSetup
    {
        private const int _minRows = 5;
        private const int _minCols = 5;
        private const int _defaultRows = 24;
        private const int _defaultCols = 80;

        public static void Execute(BbsSession session)
        {
            var originalLocation = session.CurrentLocation;
            session.CurrentLocation = Module.ConfigureEmulation;
            
            try
            {
                session.Rows = session.User.Rows;
                session.Cols = session.User.Cols;
                var detEmu = session.Io.EmulationType;
                var lastEmu = session.User.Emulation;

                if (session.Rows < _minRows)
                    session.Rows = _defaultRows;
                if (session.Cols < _minCols)
                    session.Cols = _defaultCols;

                var detected = TryAutoDetectRowsAndCols(session);

                int lastCols, lastRows;
                lastCols = session.Cols;
                lastRows = session.Rows;

                var emulation = detEmu == TerminalEmulation.Cbm ? detEmu : lastEmu;
                var cols = detected.WasDetected ? detected.Cols : lastCols;
                var rows = detected.WasDetected ? detected.Rows : lastRows;

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
                };

                ApplySettings();

                while (true)
                {
                    session.Io.OutputLine($"{Environment.NewLine} -- Terminal Settings --");
                    session.Io.OutputLine($"(C)ols (width)  : {cols}");
                    session.Io.OutputLine($"(R)ows (height) : {rows}");
                    session.Io.OutputLine($"(E)mulation     : {emulation}");
                    session.Io.OutputLine(" --- Presets --- ");
                    if (detected.WasDetected)
                        session.Io.OutputLine($"1) Auto    : {detected.Cols}c, {detected.Rows}r, {emulation}");
                    else
                        session.Io.OutputLine("1) Auto    : Not Available");
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
                            if (detected.WasDetected)
                            {
                                cols = detected.Cols;
                                rows = detected.Rows;
                                emulation = detEmu;
                                ApplySettings();
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


        private static TermSize TryAutoDetectRowsAndCols(BbsSession session)
        {
            session.Io.OutputLine("Trying to auto-detect your terminal's rows & columns.  If this appears to hang just press enter/return.");

            session.Io.OutputRaw(255, 253, 31);
            var autoDetectBytes = session.Io.InputRaw();

            int? detCols, detRows;
            detCols = detRows = null;
            if (autoDetectBytes.Length >= 3 && autoDetectBytes[0] == 255 && autoDetectBytes[1] == 251 && autoDetectBytes[2] == 31)
            {
                int offset = 6;
                if (autoDetectBytes.Length < 12)
                {
                    autoDetectBytes = session.Io.InputRaw();
                    offset = 3;
                }

                if (autoDetectBytes.Length >= offset + 4 && autoDetectBytes[0] == 255 && autoDetectBytes[1] == 250 && autoDetectBytes[2] == 31)
                {
                    detCols = (autoDetectBytes[offset] << 8) + autoDetectBytes[offset + 1];
                    detRows = (autoDetectBytes[offset + 2] << 8) + autoDetectBytes[offset + 3];
                }
            }
            session.Stream.Flush();

            var result = new TermSize
            {
                Rows = detRows ?? default,
                Cols = detCols ?? default
            };

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
