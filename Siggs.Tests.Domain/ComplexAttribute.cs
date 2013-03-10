using System;

namespace Siggs.Tests.Domain
{
    public class ComplexAttribute : Attribute
    {
        public string A { get; set; }
        public int B { get; set; }
        public string C;

        public ComplexAttribute(string a)
        {
            A = a;
            B = B;
        }
    }
}