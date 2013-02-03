using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Siggs.Tests.Domain;

namespace Siggs.Tests
{
    [TestFixture]
    public class Tests
    {
        private Type type;        

        [SetUp]
        public void SetUp()
        {
            type = typeof(Example).GetMethod("Method").GetTypeForMethodInfo();
        }
        
        [Test]
        public void CanGenerateType()
        {
            var property = type.GetProperty("message");
            Assert.AreEqual(typeof(string), property.PropertyType);
        }

        [Test]
        public void CanGenerateAttributes()
        {
            var property = type.GetProperty("message");
            var attribute = type.GetCustomAttributes().OfType<SomeAttribute>().Single();
        }

    }

    
}
