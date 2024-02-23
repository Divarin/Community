using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using System;
using System.Data.Linq.Mapping;

namespace miniBBS.Core.Models.Data
{
    [Table(Name = "Users")]
    public class User : IDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; }
        public DateTime DateAddedUtc { get; set; }
        public DateTime LastLogonUtc { get; set; }
        public int TotalLogons { get; set; }
        public AccessFlag Access { get; set; }
        public int Cols { get; set; }
        public int Rows { get; set; }
        public TerminalEmulation Emulation { get; set; }
        public int Timezone { get; set; }
        public string InternetEmail { get; set; }
    }
}
