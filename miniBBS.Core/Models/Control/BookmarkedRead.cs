using System;
using miniBBS.Core.Enums;

namespace miniBBS.Core.Models.Control
{
    public class BookmarkedRead
    {
        public BookmarkedRead()
        {

        }
        public string FullText { get; set; }
        public double Percentage { get; set; }
        public OutputHandlingFlag OutputFlags { get; set; }
        public ConsoleColor TextColor { get; set; }
    }
}
