using miniBBS.Core.Interfaces;
using System;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "Blurbs")]
    public class Blurb : IDataModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime DateAddedUtc { get; set; }
        public string BlurbText { get; set; }
    }
}
