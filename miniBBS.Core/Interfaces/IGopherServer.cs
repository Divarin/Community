using miniBBS.Core.Models.Control;

namespace miniBBS.Core.Interfaces
{
    public interface IGopherServer
    {
        void StartServer(GopherServerOptions options);
    }
}
