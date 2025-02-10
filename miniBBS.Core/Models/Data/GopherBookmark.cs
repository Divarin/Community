using miniBBS.Core.Interfaces;
using System;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "GopherBookmarks")]
    public class GopherBookmark : IDataModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public bool Private { get; set; }
        public DateTime DateCreatedUtc { get; set; }
        public string Selector { get; set; }
        public string Title { get; set; }
        public string Tags { get; set; }
    }
}
