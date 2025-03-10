using miniBBS.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Core.Models.Control
{
    public class GopherServerOptions
    {
        public SystemControlFlag SystemControl { get; set; }
        public int BbsPort { get; set; }
        public int GopherServerPort { get; set; }
    }
}
