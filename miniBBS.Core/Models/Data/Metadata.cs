using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "Metadata")]
    public class Metadata : IDataModel
    {
        public int Id { get; set; }
        public MetadataType Type { get; set; }
        public int? UserId { get; set; }
        public int? ChannelId { get; set; }
        public string Data { get; set; }
    }
}
