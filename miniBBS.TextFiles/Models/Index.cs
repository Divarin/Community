using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miniBBS.TextFiles.Models
{
    public class Index
    {
        public string Header { get; set; }
        public string Description { get; set; }
        public IEnumerable<Link> Links { get; set; }
    }
}
