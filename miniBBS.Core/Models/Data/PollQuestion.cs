using miniBBS.Core.Interfaces;
using System;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "PollQuestions")]
    public class PollQuestion : IDataModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Question { get; set; }
        public DateTime DateAddedUtc { get; set; }
        public string Answers { get; set; }
    }
}
