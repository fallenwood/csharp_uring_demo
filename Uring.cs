namespace Concurrent;

using Concurrent.Lib;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static Concurrent.Lib.Liburing;
using System;
using System.Runtime.InteropServices;


public unsafe class UringServer : IServer
{
    const int MaxEvents = 200;
    const int QUEUE_DEPTH = 256;
    const int READ_SZ = 8192;

    public IPEndPoint IPEndPoint { get; set; }

    public UringServer(IPEndPoint endPoint)
    {
        this.IPEndPoint = endPoint;
    }

    public unsafe int SetNonBlocking(int sockfd)
    {
        return fcntl(sockfd, F_SETFD, fcntl(sockfd, F_GETFD, 0) | O_NONBLOCK);
    }

    public unsafe void Run()
    {
        io_uring ring = default;
        int on = 1;

        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Any, 2300));

        var s = socket.Handle.ToInt32();

        // SetNonBlocking(s);

        setsockopt(s, SOL_SOCKET, SO_REUSEADDR, &on, sizeof(int));
        // ioctl(s, FIONBIO, &on);

        listen(s, 20);

        io_uring_queue_init(QUEUE_DEPTH, &ring, 0);

        ServerLoop(s, &ring);
    }

    public unsafe void ServerLoop(int serverSocket, io_uring* ring)
    {
        io_uring_cqe* cqe;
        sockaddr_in clientAddr = default;
        socklen_t len = sizeof(sockaddr);

        AddAcceptRequest(serverSocket, &clientAddr, &len, ring);

        Console.WriteLine("Starting server loop");
        while (true)
        {
            Console.WriteLine("Waiting cqe");
            var ret = io_uring_wait_cqe(ring, &cqe);
            Console.WriteLine($"Waiting Cqe {ret}");
            var req = (Request*)cqe->user_data;

            Console.WriteLine($"Handling event type");
            switch (req->event_type)
            {
                case EventType.EVENT_TYPE_ACCEPT:
                    {
                        Console.WriteLine("Handling EVENT_TYPE_ACCEPT");
                        AddAcceptRequest(serverSocket, &clientAddr, &len, ring);
                        AddReadRequest(cqe->res, ring);
                        // free(req);
                        break;
                    }
                case EventType.EVENT_TYPE_READ:
                    {
                        Console.WriteLine("Handling EVENT_TYPE_READ");
                        if (cqe->res == 0)
                        {
                            Console.Error.WriteLine("Empty request!");
                            break;
                        }
                        handle_client_request(req, ring);
                        // free(req->iov[0].iov_base);
                        // free(req);
                        break;
                    }
                case EventType.EVENT_TYPE_WRITE:
                    {
                        Console.WriteLine("Handling EVENT_TYPE_WRITE");
                        for (int i = 0; i < req->iovec_count; i++)
                        {
                            // free(req->iov[i].iov_base);
                        }
                        close(req->client_socket);
                        // free(req);
                        break;
                    }
            }

            Console.WriteLine("Will call io_uring_cqe_seen");
            io_uring_cqe_seen(ring, cqe);
        }

        Console.WriteLine("The loop has ended unexpectedly");
    }

    unsafe void handle_client_request(Request* req, io_uring* ring)
    {
        var sqe = io_uring_get_sqe(ring);
        req->event_type = EventType.EVENT_TYPE_WRITE;
        var bytes = Encoding.UTF8.GetBytes("Hello world");

        req->iovec_count = 1;
        req->iov = Marshal.AllocHGlobal(sizeof(iovec));
        var iov = Marshal.PtrToStructure<iovec>(req->iov);

        var iovBuf = Marshal.AllocHGlobal(READ_SZ);
        iov.iov_base = iovBuf.ToPointer();
        Marshal.Copy(bytes, 0, iovBuf, bytes.Length);

        io_uring_prep_writev(sqe, req->client_socket, (iovec*)(req->iov.ToPointer()), Convert.ToUInt32(req->iovec_count), 0);
        io_uring_sqe_set_data(sqe, req);
        io_uring_submit(ring);
    }

    unsafe int AddReadRequest(int clientSocket, io_uring* ring)
    {
        var sqe = io_uring_get_sqe(ring);
        // TODO
        var req = (Request*)Marshal.AllocHGlobal(sizeof(Request)).ToPointer();
        req->iovec_count = 1;
        req->event_type = EventType.EVENT_TYPE_READ;
        req->client_socket = clientSocket;

        req->iov = Marshal.AllocHGlobal(sizeof(iovec));
        var iov = Marshal.PtrToStructure<iovec>(req->iov);
        iov.iov_len = READ_SZ;
        iov.iov_base = Marshal.AllocHGlobal(READ_SZ).ToPointer();

        /* Linux kernel 5.5 has support for readv, but not for recv() or read() */
        io_uring_prep_readv(sqe, clientSocket, (iovec*)req->iov.ToPointer(), 1, 0);
        io_uring_sqe_set_data(sqe, &req);
        io_uring_submit(ring);
        return 0;
    }

    unsafe int AddAcceptRequest(int server_socket, sockaddr_in* client_addr, socklen_t* client_addr_len, io_uring* ring)
    {
        var sqe = io_uring_get_sqe(ring);
        Console.WriteLine($"Got SQE: {sqe->fd}");
        // TODO
        io_uring_prep_accept(
            sqe,
            server_socket,
            (sockaddr*)client_addr,
            client_addr_len,
            0);
        Console.WriteLine($"prepared sqe");
        var req = (Request*)Marshal.AllocHGlobal(sizeof(Request)).ToPointer();
        req->event_type = EventType.EVENT_TYPE_ACCEPT;
        req->iovec_count = 0;

        Console.WriteLine("set data");
        io_uring_sqe_set_data(sqe, &req);
        Console.WriteLine("submit");
        io_uring_submit(ring);

        return 0;
    }
}

[StructLayout(LayoutKind.Sequential)]
unsafe struct Request
{
    public EventType event_type { get; set; }
    public int iovec_count { get; set; }
    public int client_socket { get; set; }
    // public iovec[] iov;
    public IntPtr iov;
}

enum EventType : int
{
    EVENT_TYPE_ACCEPT,
    EVENT_TYPE_READ,
    EVENT_TYPE_WRITE,
}
