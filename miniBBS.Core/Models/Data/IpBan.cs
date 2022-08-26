using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using System;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "IpBans")]
    public class IpBan : IDataModel
    {
        public int Id { get; set; }
        public string IpMask { get; set; }
    }
}
