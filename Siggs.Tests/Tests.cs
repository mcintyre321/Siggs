﻿using System;
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
        public void CanGenerateSimpleAttributes()
        {
            var property = type.GetProperty("message");
            var attribute = property.GetCustomAttributes().OfType<SimpleAttribute>().Single();
        }
        [Test]
        public void CanGenerateComplexAttributes()
        {
            var property = type.GetProperty("message");
            var attribute = property.GetCustomAttributes().OfType<ComplexAttribute>().Single();
            Assert.AreEqual("goodbye", attribute.A);
            Assert.AreEqual(10, attribute.B);
            Assert.AreEqual("world", attribute.C);
        }

        [Test]
        public void CanGenerateComplexAttributesOfTheCorrectType()
        {
            var property = type.GetProperty("someComplexType");
            Assert.AreEqual(typeof(SomeComplexType), property.PropertyType);
        }
        [Test]
        public void TypeIsFunctional()
        {
            dynamic instance = Activator.CreateInstance(type);
            instance.someComplexType = new SomeComplexType() {Value = "hello"};
            Assert.AreEqual("hello", instance.someComplexType.Value);
        }


    }
}
