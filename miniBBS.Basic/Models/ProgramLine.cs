namespace miniBBS.Basic.Models
{
    public class ProgramLine
    {
        public ProgramLine(int lineNumber, string statement)
        {
            LineNumber = lineNumber;
            Statement = statement;
        }

        public int LineNumber { get; set; }
        public string Statement { get; set; }
    }
}
