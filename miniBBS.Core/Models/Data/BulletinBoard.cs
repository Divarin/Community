using miniBBS.Core.Interfaces;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "BulletinBoards")]
    public class BulletinBoard : IDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
