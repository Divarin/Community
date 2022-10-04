using miniBBS.Core.Interfaces;
using System;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "Chat")]
    public class Chat : IDataModel
    {
        public Chat()
        {

        }

        public int Id { get; set; }
        public int? ResponseToId { get; set; }
        public int ChannelId { get; set; }
        public int FromUserId { get; set; }
        public DateTime DateUtc { get; set; }
        public string Message { get; set; }
        public bool WebVisible { get; set; }
    }
}
