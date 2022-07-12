using miniBBS.Core.Interfaces;
using System;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "Channels")]
    public class Channel : IDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool RequiresInvite { get; set; }
        public DateTime? DateCreatedUtc { get; set; }
    }
}
