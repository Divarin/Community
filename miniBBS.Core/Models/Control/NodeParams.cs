using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using System.Net.Sockets;

namespace miniBBS.Core.Models.Control
{
    public class NodeParams
    {
        public TcpClient Client { get; set; }
        public SystemControlFlag SysControl { get; set; }
        public IMessager Messager { get; set; }
    }
}
