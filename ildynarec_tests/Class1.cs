using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using Cecil = Mono.Cecil;
using Reflection = System.Reflection;

using ILDynaRec;

namespace ildynarec_tests
{
    [TestFixture]
    public partial class RecompilerTests
    {
        Cecil.AssemblyDefinition CurrentAssembly;
        ILDynaRec.Recompiler Recompiler;
        Cecil.TypeDefinition ThisType;

        [TestFixtureSetUp]
        public void Init() {
            CurrentAssembly = Cecil.AssemblyDefinition.ReadAssembly(Reflection.Assembly.GetExecutingAssembly().Location);
            ThisType = CurrentAssembly.FindRuntimeType(GetType());

            Recompiler = new ILDynaRec.Recompiler();
        }

        class TestClass1
        {
            public int a = 0;

            public string TestMethod() {
                a = 1;
                return "a" + "b";
            }
        }

        [Test]
        public void aa() {
            var obj = new TestClass1();

            var cecilTestClass1 = CurrentAssembly.FindRuntimeType(typeof(TestClass1));
            cecilTestClass1.Methods.First(method => method.Name == "TestMethod");
            var method1 = cecilTestClass1.GetMethodByName("TestMethod");

            var dynMethod = Recompiler.RecompileMethod(method1);
            var result = dynMethod.Invoke(obj, new object[] { obj });

            Assert.AreEqual("ab", (string)result);
            Assert.AreEqual(obj.a, 1);
        }

        [Test]
        public void aa2() {
            var obj = new TestClass1();

            var cecilTestClass1 = CurrentAssembly.FindRuntimeType(typeof(TestClass1));
            cecilTestClass1.Methods.First(method => method.Name == "TestMethod");
            var method1 = cecilTestClass1.GetMethodByName("TestMethod");

            var dynMethod = Recompiler.RecompileMethod(method1);
            var result = dynMethod.Invoke(obj, new object[] { obj });

            Assert.AreEqual("ab", (string)result);
            Assert.AreEqual(obj.a, 1);
        }
    }
}
