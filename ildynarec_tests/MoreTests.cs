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
    public partial class RecompilerTests
    {
        class TestClassA
        {
            public TestClassB obj;

            public TestClassB createObj() {
                obj = new TestClassB();
                return obj;
            }

            public T genericGetObj<T>(T aa) where T : class {
                return obj as T;
            }

            public int dotest() {
                createObj();
                var o = genericGetObj<TestClassB>(null);
                if (o != null) {
                    return o.c.testField;
                }
                else {
                    return 0;
                }
            }
        }

        class TestClassB
        {
            public TestClassC c {
                get {
                    return new TestClassC();
                }
            }
        }

        class TestClassC
        {
            public int testField {
                get {
                    return 1337;
                }
            }
        }

        [Test]
        public void TestInnerAccess() {
            var obj = new TestClassA();
            var cecilType = CurrentAssembly.FindRuntimeType(typeof(TestClassA));

            var met2 = Recompiler.RecompileMethod(cecilType.GetMethodByName("dotest"));
            var result = met2.Invoke(this, new object[] { obj });

            Assert.NotNull(obj.obj);
            Assert.AreEqual(1337, (int)result);
        }
    }
}