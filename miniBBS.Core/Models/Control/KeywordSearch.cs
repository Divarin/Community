using miniBBS.Core.Enums;

namespace miniBBS.Core.Models.Control
{
    public class KeywordSearch
    {
        public string Keyword { get; set; }
        public SearchFrom From { get; set; }
        public int LastMatchLineNumber { get; set; }
    }
}
