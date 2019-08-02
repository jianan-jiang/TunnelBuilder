using System;
using NurbsMesher;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NurbsMesherTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethodShouldPass()
        {
            var nurbsMesher = new NurbsMesher.NurbsMesher();
            Assert.AreEqual("Hello World", nurbsMesher.HelloWorld());
        }

        [TestMethod]
        public void TestMethodShouldFail()
        {
            var nurbsMesher = new NurbsMesher.NurbsMesher();
            Assert.AreEqual("HelloWorld", nurbsMesher.HelloWorld());
        }
    }
}
