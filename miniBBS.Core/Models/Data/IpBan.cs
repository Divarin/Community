using miniBBS.Core.Interfaces;
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
