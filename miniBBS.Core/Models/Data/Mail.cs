using miniBBS.Core.Interfaces;
using System;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "Mail")]
    public class Mail : IDataModel
    {
        public int Id { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public DateTime SentUtc { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public bool Read { get; set; }
    }
}
