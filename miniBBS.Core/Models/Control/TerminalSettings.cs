using miniBBS.Core.Enums;

namespace miniBBS.Core.Models.Control
{
    public class TerminalSettings
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public int Cols { get; set; }
        public int Rows { get; set; }
        public TerminalEmulation Emulation { get; set; }
    }
}
