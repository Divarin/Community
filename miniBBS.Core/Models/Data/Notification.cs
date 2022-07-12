using miniBBS.Core.Interfaces;
using System;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "Notifications")]
    public class Notification : IDataModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime DateSentUtc { get; set; }
        public string Message { get; set; }
    }
}
