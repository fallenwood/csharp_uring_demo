using System.Net;
using System.Net.Sockets;
using System.Text;
using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace Concurrent;

public class PollServer : IServer
{
    public IPEndPoint IPEndPoint { get; set; }

    public PollServer(IPEndPoint endPoint)
    {
        this.IPEndPoint = endPoint;
    }

    public unsafe void Run()
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(IPEndPoint);

        var s = socket.Handle.ToInt32();
        int on = 1;

        setsockopt(s, SOL_SOCKET, SO_REUSEADDR, &on, sizeof(int));

        ioctl(s, FIONBIO, &on);

        listen(s, 20);

        var fds = stackalloc pollfd[200];
        ulong_t nfds = 1;

        fds[0].fd = s;
        fds[0].events = POLLIN;

        var bytes = Encoding.UTF8.GetBytes("Hello world!");

        while (true)
        {
            poll(fds, nfds, -1);

            var currentSize = nfds;

            for (ulong_t i = 0;i < currentSize; i++)
            {
                if (fds[i].revents == 0)
                {
                    continue;
                }

                if (fds[i].revents != POLLIN)
                {
                    break;
                }
                
                if (fds[i].fd == s)
                {
                    int newSock = -1;
                    do
                    {
                        newSock = accept(s, null, null);
                        fds[nfds].fd = newSock;
                        fds[nfds].events = POLLIN;
                        nfds++;
                        // sockaddr addr;
                        // socklen_t size = sizeof(sockaddr);
                        // var newSock = accept(s, &addr, &size);
                    } while (newSock != -1);
                }
                else
                {
                    fixed (byte* buffer = bytes)
                    {
                        write(fds[i].fd, buffer, bytes.Length);
                    }

                    close(fds[i].fd);
                    fds[i].fd = -1;
                }
            }
        }
    }
}
