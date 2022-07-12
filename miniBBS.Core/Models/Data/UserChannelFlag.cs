using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "UserChannelFlags")]
    public class UserChannelFlag : IDataModel
    {
        public UserChannelFlag()
        {

        }
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ChannelId { get; set; }
        public UCFlag Flags { get; set; }
        public int LastReadMessageNumber { get; set; }
    }
}
