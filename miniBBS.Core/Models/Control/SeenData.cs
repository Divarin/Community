using System;

namespace miniBBS.Core.Models.Control
{
    public class SeenData
    {
        public DateTime SessionsStartUtc { get; set; }
        public DateTime SessionEndUtc { get; set; }
        public string QuitMessage { get; set; }
    }
}
