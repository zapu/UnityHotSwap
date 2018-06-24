using System;
using System.Collections.Generic;

using ILDynaRec;
using NUnit.Framework;
using System.Linq;
using Cecil = Mono.Cecil;
using Reflection = System.Reflection;

namespace ildynarec_tests
{
    [TestFixture]
    public partial class RecompilerTests
    {
        private Cecil.AssemblyDefinition CurrentAssembly;
        private ILDynaRec.Recompiler Recompiler;
        private Cecil.TypeDefinition ThisType;

        [TestFixtureSetUp]
        public void Init() {
            CurrentAssembly = CecilExtensions.CurrentAssembly;
            ThisType = CurrentAssembly.FindRuntimeType(GetType());

            Recompiler = new ILDynaRec.Recompiler();
        }

        private class TestClass1
        {
            public int a = 0;

            public string TestMethod() {
                a = 1;
                return "a" + "b";
            }

            public static string FindSomething<T>(out T val) where T : class, new() {
                val = new T();
                return typeof(T).ToString();
            }

            public static string DoSomethingWithGeneric() {
                System.Collections.ArrayList res;
                var t = FindSomething(out res);
                return t;
            }

            static Dictionary<Type, int> m_dict = new Dictionary<Type, int>();
            public static string DoSomethingGenericWithGeneric<T>() where T : class, new() {
                T res;
                var t = FindSomething<T>(out res);
                m_dict[typeof(T)] = 1;
                return m_dict[typeof(T)].ToString();
            }

            List<string> m_strings = new List<string>();
            public bool TestForeach() {
                foreach (var str in m_strings) {
                    if (str == "") {
                        return true;
                    }
                }
                return false;
            }
        }

        [Test]
        public void aa() {
            var obj = new TestClass1();

            var cecilTestClass1 = CurrentAssembly.FindRuntimeType(typeof(TestClass1));
            var method1 = cecilTestClass1.GetMethodByName("TestMethod");

            var dynMethod = Recompiler.RecompileMethod(method1);
            var result = dynMethod.Invoke(obj, new object[] { obj });

            Assert.AreEqual("ab", (string)result);
            Assert.AreEqual(obj.a, 1);
        }

        [Test]
        public void OutGenericParam() {
            var obj = new TestClass1();

            var cecilTestClass1 = CurrentAssembly.FindRuntimeType(typeof(TestClass1));
            var method1 = cecilTestClass1.GetMethodByName("FindSomething");

            var dynMethod = Recompiler.RecompileMethod(method1);
            var pms = new object[] { null };
            var result = dynMethod.Invoke(null, pms);

            Assert.Equals(typeof(TestClass1), pms[0].GetType());
            Assert.AreEqual(obj.GetType().ToString(), (string)result);
        }

        [Test]
        public void CallGenericMethod() {
            var cecilTestClass1 = CurrentAssembly.FindRuntimeType(typeof(TestClass1));
            var method1 = cecilTestClass1.GetMethodByName("DoSomethingWithGeneric");

            var dynMethod = Recompiler.RecompileMethod(method1);
            var result = dynMethod.Invoke(null, new object[] { });

            Assert.AreEqual(typeof(System.Collections.ArrayList).ToString(), (string)result);
        }

        [Test]
        public void CallGenericMethodInGenericMethod() {
            var cecilTestClass1 = CurrentAssembly.FindRuntimeType(typeof(TestClass1));
            var method1 = cecilTestClass1.GetMethodByName("DoSomethingGenericWithGeneric");

            var dynMethod = Recompiler.RecompileMethod(method1);
            HotPatcher.TestPrepareMethod(dynMethod);

            var result = dynMethod.Invoke(null, new object[] { });

            Assert.AreEqual("1", (string)result);
        }

        [Test]
        public void Foreach() {
            var obj = new TestClass1();

            var cecilTestClass1 = CurrentAssembly.FindRuntimeType(typeof(TestClass1));
            var method1 = cecilTestClass1.GetMethodByName("TestForeach");

            var dynMethod = Recompiler.RecompileMethod(method1);
            var result = dynMethod.Invoke(obj, new object[] { obj });

            Assert.AreEqual(false, (bool)result);
        }

        static int MethodWithDefaultArg(int v = 5) { return v; }
        static int CallerMethodWithDefaultArg() {
            return MethodWithDefaultArg();
        }

        [Test]
        public void DefaultArguments() {
            var cecilTestClass1 = CurrentAssembly.FindRuntimeType(typeof(RecompilerTests));
            var method1 = cecilTestClass1.GetMethodByName("CallerMethodWithDefaultArg");
            var dynm = Recompiler.RecompileMethod(method1);
            var result = dynm.Invoke(null, new object[] { });
            Assert.AreEqual(5, (int)result);
        }
    }
}