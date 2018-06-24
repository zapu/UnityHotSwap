using System;
using System.Collections.Generic;
using System.Linq;

using Cecil = Mono.Cecil;
using Reflection = System.Reflection;

namespace ildynarec_tests
{
    public static class CecilExtensions
    {
        public static IEnumerable<Cecil.TypeDefinition> IterateNestedTypes(
            this Cecil.TypeDefinition type) {
            foreach (var nestedType in type.NestedTypes) {
                yield return nestedType;

                foreach (var nested in IterateNestedTypes(nestedType)) {
                    yield return nestedType;
                }
            }
        }

        public static IEnumerable<Cecil.TypeDefinition> IterateTypes(
            this Cecil.AssemblyDefinition assembly) {
            foreach (var module in assembly.Modules) {
                foreach (var type in module.Types) {
                    yield return type;

                    foreach (var nestedType in IterateNestedTypes(type)) {
                        yield return nestedType;
                    }
                }
            }
        }

        public static Cecil.TypeDefinition FindRuntimeType(
            this Cecil.AssemblyDefinition assembly, Type runtimeType) {
            var typename = runtimeType.FullName.Replace('+', '/');
            return assembly.IterateTypes().FirstOrDefault(type => type.FullName == typename);
        }

        private static Cecil.AssemblyDefinition s_currentAsm;
        public static Cecil.AssemblyDefinition CurrentAssembly {
            get {
                if(s_currentAsm == null) {
                    s_currentAsm = Cecil.AssemblyDefinition.ReadAssembly(Reflection.Assembly.GetExecutingAssembly().Location);
                }
                return s_currentAsm;
            }
        }

        public static Cecil.MethodDefinition GetMethodByName(this Cecil.TypeDefinition type, string name) {
            return type.Methods.FirstOrDefault(method => method.Name == name);
        }
    }
}