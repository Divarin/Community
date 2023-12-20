using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Basic.Executors
{
    public static class Find
    {
        public static void Execute(BbsSession session, string term, SortedList<int, string> progLines)
        {
            if (string.IsNullOrWhiteSpace(term))
                session.Io.OutputLine("What am I looking for?");
            else
            {
                var lines = progLines?.Where(l => l.Value.ToLower().Contains(term));
                var highlightColor = session.Io.GetForeground() == ConsoleColor.Magenta ? ConsoleColor.Yellow : ConsoleColor.Magenta;
                
                if (true == lines?.Any())
                {
                    var builder = new StringBuilder();
                    foreach (var l in lines)
                    {
                        string line;
                        switch (session.Io.EmulationType)
                        {
                            case TerminalEmulation.Ansi:
                            case TerminalEmulation.Cbm:
                                line = HighlightMatch(l.Value, term, highlightColor);
                                break;
                            case TerminalEmulation.Atascii:
                                line = InverseMatch(l.Value, term);
                                break;
                            default:
                                line = l.Value;
                                break;
                        }
                        builder.AppendLine($"{l.Key} {line}");
                    }
                    session.Io.Output(builder.ToString());
                }
            }
        }

        private static string HighlightMatch(string line, string term, ConsoleColor highlightColor) =>
            line.Replace(term, term.Color(highlightColor));

        private static string InverseMatch(string line, string term) =>
            line.Replace(term, $"{Constants.Inverser}{term}{Constants.Inverser}");
    }
}
