using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siggs.Tests.Domain
{
    public class Example
    {
        public void Method([Simple] [Complex("goodbye", B = "cruel", C = "world")] string message)
        {
            
        }
    }
}
