namespace miniBBS.Basic.Models
{
    public struct EditResult
    {
        public int LineNumber { get; set; }
        public string OriginalLine { get; set; }
        public string NewLine { get; set; }
        public bool Aborted { get; internal set; }
    }
}
