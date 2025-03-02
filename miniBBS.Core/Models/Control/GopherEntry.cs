using miniBBS.Core.Enums;

namespace miniBBS.Core.Models.Control
{
    public class GopherEntry
    {
        public GopherEntryType EntryType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }
        public string Host { get; set; }
        public int? Port { get; set; }

        /// <summary>
        /// If this entry is a user-selectable entry (not just information) then this is the number
        /// the user will type in to go to the entry.
        /// </summary>
        public int? Number { get; set; }
        public string CachedDocument { get; set; }
    }
}
