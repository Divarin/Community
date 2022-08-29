using miniBBS.Core.Interfaces;
using System;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "PinnedMessages")]
    public class PinnedMessage : IDataModel
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public int ChannelId { get; set; }
        public int PinnedByUserId { get; set; }
        public bool Private { get; set; }
        public DateTime DatePinnedUtc { get; set; }
    }
}
