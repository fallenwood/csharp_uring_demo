using System.Runtime.InteropServices;
using Tmds.Linux;

namespace Concurrent.Lib;

public unsafe struct io_uring_sq
{
    public uint* khead { get; set; }
    public uint* ktail { get; set; }
    public uint* kring_mask { get; set; }
    public uint* kring_entries { get; set; }
    public uint* kflags { get; set; }
    public uint* kdropped { get; set; }
    public uint* array { get; set; }
    public io_uring_sqe* sqes { get; set; }

    public uint sqe_head { get; set; }
    public uint sqe_tail { get; set; }

    public size_t ring_sz { get; set; }
    public void* ring_ptr { get; set; }

    public fixed byte pad[4];
};

public unsafe struct io_uring
{
    public io_uring_sq sq { get; set; }
    public io_uring_cq cq { get; set; }
    public uint flags { get; set; }
    public int ring_fd { get; set; }

    public uint features { get; set; }
    public int enter_ring_fd { get; set; }
    public byte int_flags { get; set; }
    public fixed byte pad[3];
    public uint pad2 { get; set; }
}

public unsafe struct io_uring_cq
{
    public uint* khead { get; set; }
    public uint* ktail { get; set; }
    public uint* kring_mask { get; set; }
    public uint* kring_entries { get; set; }
    public uint* kflags { get; set; }
    public uint* koverflow { get; set; }
    public io_uring_cqe* cqes { get; set; }

    public size_t ring_sz { get; set; }
    public void* ring_ptr { get; set; }

    public fixed byte pad[4];
};

public static class Liburing
{
    [DllImport("liburing.so.2", SetLastError = true)]
    public static unsafe extern int io_uring_queue_init(uint entries, io_uring *ring, uint flags);

    [DllImport("liburing.so.2", SetLastError = true)]
    public static unsafe extern int io_uring_wait_cqe(io_uring* ring, io_uring_cqe** cqe_ptr);

    [DllImport("liburing.so.2", SetLastError = true)]
    public static unsafe extern void io_uring_cqe_seen(io_uring* ring, io_uring_cqe* cqe);

    [DllImport("liburing.so.2", SetLastError = true)]
    public static unsafe extern io_uring_sqe *io_uring_get_sqe(io_uring *ring);

    [DllImport("liburing.so.2", SetLastError = true)]
    public static unsafe extern void io_uring_prep_accept(io_uring_sqe* sqe, int fd, sockaddr* addr, socklen_t* addrlen, int flags);

    [DllImport("liburing.so.2", SetLastError = true)]
    public static unsafe extern void io_uring_sqe_set_data(io_uring_sqe* sqe, void* data);

    [DllImport("liburing.so.2", SetLastError = true)]
    public static unsafe extern void io_uring_submit(io_uring* ring);

    [DllImport("liburing.so.2", SetLastError = true)]
    public static unsafe extern void io_uring_prep_readv(io_uring_sqe* sqe, int fd, iovec* iovecs, uint nr_vecs, ulong offset);

    [DllImport("liburing.so.2", SetLastError = true)]
    public static unsafe extern void io_uring_prep_writev(io_uring_sqe* sqe, int fd, iovec* iovecs, uint nr_vecs, ulong offset);
}
