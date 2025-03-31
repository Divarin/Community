using miniBBS.Core.Interfaces;
using System;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "BbsList")]
    public class Bbs : IDataModel
    {
        public int Id { get; set; }
        public int AddedByUserId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Port { get; set; }
        public string Sysop { get; set; }
        public string Software { get; set; }
        public string Emulations { get; set; }
        public string Description { get; set; }
        public DateTime DateAddedUtc { get; set; }
    }
}
