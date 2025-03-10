using System;
using System.Net.Sockets;

namespace miniBBS.Services
{
    public class TcpClientFactory
    {
        private TcpListener _listener;
        public TcpClient Client { get; private set; }

        public TcpClientFactory(TcpListener listener)
        {
            _listener = listener;
            Client = null;
        }

        public void AwaitConnection()
        {
            _listener.BeginAcceptTcpClient(OnAccepted, null);
        }

        private void OnAccepted(IAsyncResult result)
        {
            Client = _listener.EndAcceptTcpClient(result);
        }
    }
}
