using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace Concurrent
{
    public class EPoll
    {
        const int MaxEvents = 200;

        public unsafe int SetNonBlocking(int sockfd)
        {
            return fcntl(sockfd, F_SETFD, fcntl(sockfd, F_GETFD, 0) | O_NONBLOCK);
        }

        public unsafe int EpollCtlAdd(int epfd, int fd, int events)
        {
            epoll_event ev;
            ev.events = events;
            ev.data.fd = fd;
            return epoll_ctl(epfd, EPOLL_CTL_ADD, fd, &ev);
        }

        public unsafe void Run()
        {
            var events = stackalloc epoll_event[MaxEvents];
            var bytes = Encoding.UTF8.GetBytes("Hello world!");
            int on = 1;

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 2300));

            var s = socket.Handle.ToInt32();

            SetNonBlocking(s);

            setsockopt(s, SOL_SOCKET, SO_REUSEADDR, &on, sizeof(int));
            ioctl(s, FIONBIO, &on);

            listen(s, 20);

            var epfd = epoll_create(1);
            EpollCtlAdd(epfd, s, EPOLLIN | EPOLLOUT | EPOLLET);

            while (true)
            {
                var nfds = epoll_wait(epfd, events, MaxEvents, -1);

                for (var i = 0;i < nfds; i++)
                {
                    if (events[i].data.fd == s)
                    {
                        sockaddr addr;
                        socklen_t socklen = sizeof(sockaddr);
                        var newSock = accept(s, &addr, &socklen);

                        // inet_ntop
                        SetNonBlocking(newSock);
                        EpollCtlAdd(epfd, newSock, EPOLLIN | EPOLLET | EPOLLRDHUP | EPOLLHUP);
                    }
                    else if ((events[i].events & EPOLLIN) != 0)
                    {
                        fixed (byte* buffer = bytes)
                        {
                            write(events[i].data.fd, buffer, bytes.Length);
                        }
                        epoll_ctl(epfd, EPOLL_CTL_DEL, events[i].data.fd, null);
                        close(events[i].data.fd);
                    }

                    if ((events[i].events & (EPOLLRDHUP | EPOLLHUP)) != 0) {
                        epoll_ctl(epfd, EPOLL_CTL_DEL, events[i].data.fd, null);
                        close(events[i].data.fd);
                        continue;
                    }
                }
            }
        }
    }
}
