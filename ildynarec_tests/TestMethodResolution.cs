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
        class MultipleOverloadedMethodsClass
        {
            int sum(int a, int b) {
                return a + b;
            }

            string sum(string a, string b) {
                return String.Format("{0}{1}", a, b);
            }

            public bool test() {
                int a = sum(10, 15);
                string b = sum("a", "b");

                return a == 25 && b == "ab";
            }
        }

        [Test]
        public void TestOverloadedMethods() {
            var obj = new MultipleOverloadedMethodsClass();
            var cecilType = CurrentAssembly.FindRuntimeType(typeof(MultipleOverloadedMethodsClass));

            var met = Recompiler.RecompileMethod(cecilType.GetMethodByName("test"));
            var result = met.Invoke(this, new object[] { obj });

            Assert.AreEqual(true, (bool)result);
        }

        class MultipleOverloadedGenericMethodsClass
        {
            bool compare<T>(T a) {
                return true;
            }

            bool compare<T>(T a, T b) {
                return a.Equals(b);
            }

            bool compare<T>(T a, T b, T c) {
                return a.Equals(b) && b.Equals(c);
            }

            int compare<T, TResult>(T a, T b, int c) {
                if (a.Equals(b)) {
                    return 0;
                }
                else {
                    return c;
                }
            }

            public bool test() {
                int a = 10;
                int b = 15;
                int c = 15;

                return compare(10) && !compare(a, b) && compare(b, c) && !compare(a, b, c);
            }

            public bool test2() {
                int a = 10;
                int b = 15;

                return compare<int, int>(a, b, 10) == 10;
            }
        }

        [Test]
        public void TestOverloadedGenericMethods() {
            var obj = new MultipleOverloadedGenericMethodsClass();
            var cecilType = CurrentAssembly.FindRuntimeType(typeof(MultipleOverloadedGenericMethodsClass));

            var met = Recompiler.RecompileMethod(cecilType.GetMethodByName("test"));
            var result = met.Invoke(obj, new object[] { obj });

            Assert.AreEqual(true, (bool)result);

            met = Recompiler.RecompileMethod(cecilType.GetMethodByName("test2"));
            result = met.Invoke(obj, new object[] { obj });

            Assert.AreEqual(true, (bool)result);
        }
    }
}