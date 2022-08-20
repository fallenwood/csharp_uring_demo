using System.Text;
using static Tmds.Linux.LibC;

namespace Concurrent
{
    class Program
    {
        unsafe static void Main(string[] args)
        {
            var select = new Select();
            select.Run();
        }
    }
}