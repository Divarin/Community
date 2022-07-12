using miniBBS.Core.Interfaces;
using System;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "Logs")]
    public class LogEntry : IDataModel
    {
        public int Id { get; set; }
        public Guid? SessionId { get; set; }
        public string IpAddress { get; set; }
        public int? UserId { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string Message { get; set; }
    }
}
