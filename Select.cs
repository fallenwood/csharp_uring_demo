using System.Net;
using System.Net.Sockets;
using System.Text;
using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace Concurrent
{
    public class Select
    {
        public unsafe void Run()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 2300));

            var s = socket.Handle.ToInt32();

            listen(s, 20);

            while (true)
            {
                sockaddr addr;
                socklen_t size = sizeof(sockaddr);
                var newSock = accept(s, &addr, &size);

                var bytes = Encoding.UTF8.GetBytes("Hello world!");
                fixed (byte* buffer = bytes)
                {
                    write(newSock, buffer, bytes.Length);
                }
            }
        }
    }
}
