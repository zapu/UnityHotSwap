using ILDynaRec;
using NUnit.Framework;
using System.Linq;
using Cecil = Mono.Cecil;
using Reflection = System.Reflection;

namespace ildynarec_tests
{
    public class ArgClassTest { }
    public static class ClassUnderTest111
    {
        public static void Draw2DArray<T>(T[] arr, int sizeX, int sizeY) {

        }

        public static void TestThing(ArgClassTest arg1, out byte[] ret) {
            ret = new byte[] { 0x00 };
        }
    }


    [TestFixture]
    public class TypeResolverTest
    {
        void TryMethod(System.Type type, string methodName) {
            var asm = CecilExtensions.CurrentAssembly;
            var recompiler = new Recompiler();
            var cecilType = asm.FindRuntimeType(type);
            var cecilMethod = cecilType.GetMethodByName(methodName);

            var realType = recompiler.FindType(cecilType);
            Assert.AreEqual(type, realType);

            var realMethod = recompiler.FindMethod(realType, cecilMethod);
            Assert.NotNull(realMethod);
        }

        [Test]
        public void TestBasic() {
            TryMethod(typeof(ClassUnderTest111), "TestThing");
            //TryMethod(typeof(ClassUnderTest111), "Draw2DArray");
        }
    }
}
