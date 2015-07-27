using System;

using System;
using System.Collections;
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
        class GenericClass<T> where T : class
        {
            public static List<T> List = new List<T>();

            public void add() {
                List.Add(this as T);
            }
        }

        class GenericDerivedClass1 : GenericClass<GenericDerivedClass1>
        {
            public bool test() {
                return List.Count == 1 && List[0] == this;
            }
        }

        class GenericDerivedClass2 : GenericClass<GenericDerivedClass2>
        {
            public bool test() {
                return List.Count == 2 && List[0] == this && List[1] == this;
            }
        }

        [Test]
        public void TestGenericClasses() {
            var obj1 = new GenericDerivedClass1();
            var obj2 = new GenericDerivedClass2();
            var cecilType1 = CurrentAssembly.FindRuntimeType(typeof(GenericDerivedClass1));
            var cecilType2 = CurrentAssembly.FindRuntimeType(typeof(GenericDerivedClass2));

            obj1.add();

            obj2.add();
            obj2.add();

            var met = Recompiler.RecompileMethod(cecilType1.GetMethodByName("test"));
            var result = met.Invoke(obj1, new object[] { obj1 });

            Assert.IsTrue((bool)result);

            met = Recompiler.RecompileMethod(cecilType2.GetMethodByName("test"));
            result = met.Invoke(obj2, new object[] { obj2 });

            //obj2 was added twice. Test if target method bound to proper field
            Assert.IsTrue((bool)result);
        }
    }
}

