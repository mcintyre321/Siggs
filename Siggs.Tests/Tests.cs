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
        /// <summary>
        /// The aim of Siggs is to generate a Type at runtime which can be used to modelbind against a method
        /// </summary>
        private Type generatedTypeForMethod;

        private Type generatedTypeForProperty;

        [SetUp]
        public void SetUp()
        {
            generatedTypeForMethod = typeof (Example).GetMethod("Method").GetTypeForMethodInfo();
            generatedTypeForProperty = typeof (Example).GetProperty("Property").GetSetMethod().GetTypeForMethodInfo();

        }
    

    [Test]
        public void CanGenerateType()
        {
            var property = generatedTypeForMethod.GetProperty("message");
            Assert.AreEqual(typeof(string), property.PropertyType);
        }

        [Test]
        public void CanGenerateSimpleAttributes()
        {
            var property = generatedTypeForMethod.GetProperty("message");
            var attribute = property.GetCustomAttributes().OfType<SimpleAttribute>().Single();
        }
        [Test]
        public void CanGenerateComplexAttributes()
        {
            var property = generatedTypeForMethod.GetProperty("message");
            var attribute = property.GetCustomAttributes().OfType<ComplexAttribute>().Single();
            Assert.AreEqual("goodbye", attribute.A);
            Assert.AreEqual(10, attribute.B);
            Assert.AreEqual("world", attribute.C);
        }

        [Test]
        public void CanGenerateComplexAttributesOfTheCorrectType()
        {
            var property = generatedTypeForMethod.GetProperty("someComplexType");
            Assert.AreEqual(typeof(SomeComplexType), property.PropertyType);
        }
        [Test]
        public void MethodTypeIsFunctional()
        {
            dynamic instance = Activator.CreateInstance(generatedTypeForMethod);
            instance.someComplexType = new SomeComplexType() { Value = "hello" };
            Assert.AreEqual("hello", instance.someComplexType.Value);
        }

        [Test]
        public void PropertyTypeIsFunctional()
        {
            dynamic instance = Activator.CreateInstance(generatedTypeForProperty);
            instance.Property = "hello";
            Assert.AreEqual("hello", instance.Property);
        }


        [Test]
        public void WhenCalledAgainstASetterThePropertyOnTheGeneratedTypeIsNamedAfterTheProperty()
        {
            var generatedName = this.generatedTypeForProperty.GetProperties().Single().Name;
            Assert.AreEqual("Property", generatedName);
        }

    }
}
