using miniBBS.Core.Interfaces;
using System;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "CalendarItems")]
    public class CalendarItem : IDataModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string EventTime { get; set; }
        public int? ChannelId { get; set; }
        public string Topic { get; set; }
        public DateTime DateCreatedUtc { get; set; }
    }
}
