using System.Net;

namespace Concurrent;

public interface IServer
{
    IPEndPoint IPEndPoint { get; set; }

    void Run();
}
