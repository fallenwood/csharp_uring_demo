using System.Text;
using static Tmds.Linux.LibC;

namespace Concurrent
{
    class Program
    {
        unsafe static void Main(string[] args)
        {
#if false
            var select = new Poll();
            select.Run();
#endif
            var select = new EPoll();
            select.Run();
        }
    }
}