using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siggs.Tests.Domain
{
    public class Example
    {
        public void Method([SomeAttribute] string message)
        {
            
        }
    }

    public class SomeAttribute : Attribute
    {
    }
}
