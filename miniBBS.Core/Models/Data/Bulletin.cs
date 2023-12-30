using miniBBS.Core.Interfaces;
using System;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "Bulletins")]
    public class Bulletin : IDataModel
    {
        public int Id { get; set; }
        public int FromUserId { get; set; }
        public int? ResponseToId { get; set; }
        public int? OriginalId { get; set; }
        public int? ToUserId { get; set; }
        public DateTime DateUtc { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public int BoardId { get; set; }
    }
}
