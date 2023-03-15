using System;
using System.Net;

namespace Concurrent;

class Program
{
    unsafe static void Main(string[] args)
    {
        var endpoint = new IPEndPoint(IPAddress.Any, 2300);

#if false
        Console.WriteLine("Running Poll Server");
        var server = new PollServer(endpoint);
        server.Run();
#endif

#if false
        Console.WriteLine("Running EPoll Server");
        var server = new EpollServer(endpoint);
        server.Run();
#endif

#if true
        Console.WriteLine("Running IO Uring Server");
        var server = new UringServer(endpoint);
        
        server.Run();
#endif
    }
}
