using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siggs.Tests.Domain
{
    public class Example
    {
        public void Method([Simple] [Complex("goodbye", B = 10, C = "world")] string message, SomeComplexType someComplexType)
        {
            
        }
    }
}
