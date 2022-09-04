using miniBBS.Core.Interfaces;
using System;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "PollVotes")]
    public class PollVote : IDataModel
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public int UserId { get; set; }
        public DateTime DateAddedUtc { get; set; }
        public string Answer { get; set; }
    }
}
