using System;
using System.Linq;
using System.Collections.Generic;

using Cecil = Mono.Cecil;
using BindingFlags = System.Reflection.BindingFlags;

namespace ILDynaRec
{
    public class AssemblyInstrumentedException : Exception
    {
        public AssemblyInstrumentedException(string filename)
            : base(String.Format("Assembly is hot-patch instrumented when it was not expected to be. {0}", filename)) {

        }
    }

    public class HotPatcher
    {
        Cecil.AssemblyDefinition currentAssembly;
        Recompiler recompiler = new Recompiler();

        public HotPatcher() {

        }

        public void Start(string assemblyFilename) {
            //Keep the "noninstrumented" version of assembly
            currentAssembly = Cecil.AssemblyDefinition.ReadAssembly(assemblyFilename);

            bool instrumented = IsInstruemnted(currentAssembly);

            InitialLoadSignatures();

            if (!instrumented) {
                //Load assembly again, instrument, save and let unity use it
                var asmToInstrument = Cecil.AssemblyDefinition.ReadAssembly(assemblyFilename);
                Instrument.InstrumentAssembly(asmToInstrument);
                asmToInstrument.Write(assemblyFilename);

                Debug.Log("HotPatcher: Assembly instrumented.");
            }
            else {
                Debug.Log("HotPatcher: Assembly was already instrumented.");
            }
        }

        Cecil.TypeDefinition FindType(Cecil.AssemblyDefinition asm, string name) {
            return asm.MainModule.Types.FirstOrDefault(type => type.Name == name);
        }

        IEnumerable<Cecil.MethodDefinition> IterateMethods(Cecil.AssemblyDefinition asm) {
            foreach (var module in asm.Modules) {
                foreach (var type in module.Types) {
                    foreach (var method in type.Methods) {
                        if (!method.HasBody) {
                            continue;
                        }

                        yield return method;
                    }
                }
            }
        }

        /// <summary>
        /// Check if assembly is instrumented
        /// </summary>
        static bool IsInstruemnted(Cecil.AssemblyDefinition assembly) {
            foreach (var module in assembly.Modules) {
                foreach (var type in module.Types) {
                    foreach (var field in type.Fields) {
                        if (field.IsHotpatchField()) {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        int GenBodyHashcode(Cecil.MethodDefinition method) {
            return ConcatBody(method).GetHashCode();
        }

        /// <summary>
        /// Generate "position independent" representation of IL.
        /// 
        /// We need to do this extra work because even if we skip 
        /// instrumentation prologue of functions when iterating
        /// instructions, typical "toString()" representation of 
        /// Cecil.Cil.Instructions contains absolute instruction
        /// positions.
        /// </summary>
        string ConcatBody(Cecil.MethodDefinition method) {
            const string skipAddr = "IL_0000: ";

            var sb = new System.Text.StringBuilder();
            foreach (var ins in Instrument.IterateInstructions(method)) {
                if (ins.Operand is Cecil.Cil.Instruction) {
                    // instructions that refer to other instructions, like
                    // branches.

                    var insOperand = (Cecil.Cil.Instruction)(ins.Operand);
                    var offset = insOperand.Offset - ins.Offset;

                    sb.AppendLine(String.Format("{0} {1}", ins.OpCode, offset));
                }
                else {
                    sb.AppendLine(ins.ToString().Substring(skipAddr.Length));
                }
            }
            return sb.ToString();
        }

        Dictionary<string, int> methodHashes = new Dictionary<string, int>();
        Dictionary<string, string> methodBodies = new Dictionary<string, string>();

        void InitialLoadSignatures() {
            foreach (var method in IterateMethods(currentAssembly)) {
                methodHashes[method.FullName] = GenBodyHashcode(method);
                methodBodies[method.FullName] = ConcatBody(method);
            }
        }

        public void HotPatch(string assemblyFilename) {
            Debug.LogFormat("Started hotpatching {0}", assemblyFilename);

            var newAssembly = Cecil.AssemblyDefinition.ReadAssembly(assemblyFilename);

            foreach (var method in IterateMethods(newAssembly)) {
                int hashcode;
                if (methodHashes.TryGetValue(method.FullName, out hashcode)) {
                    if (hashcode == GenBodyHashcode(method)) {
                        continue;
                    }

                    string oldBody = methodBodies[method.FullName];
                    string newBody = ConcatBody(method);

                    Debug.Log("Trying to hotpatch " + method.FullName);
                    var ourType = recompiler.FindType(method.DeclaringType);
                    if (ourType == null) {
                        continue;
                    }

                    var patchField = ourType.GetField(method.GetHotpatchFieldName(), BindingFlags.Static | BindingFlags.NonPublic);
                    if (patchField == null) {
                        Debug.LogWarningFormat("Cannot patch {0} - function is not hotpatchable", method.FullName);
                        continue;
                    }

                    Debug.Trace("Hotswapping {0}", method.FullName);
                    Debug.Trace("Method signature was: {0}, is: {1}", methodHashes[method.FullName], GenBodyHashcode(method));

                    Debug.Trace("Method body was:");
                    Debug.Trace(methodBodies[method.FullName]);

                    Debug.Trace("Method body is:");
                    Debug.Trace(ConcatBody(method));

                    try {
                        var dynmethod = recompiler.RecompileMethod(method);
                        if (dynmethod != null) {
                            patchField.SetValue(null, dynmethod);

                            methodHashes[method.FullName] = GenBodyHashcode(method);
                            methodBodies[method.FullName] = ConcatBody(method);
                        }
                    }
                    catch (Exception e) {
                        Debug.LogWarningFormat("Failed to patch {0} - {1}. See full stacktrace in Temp/hotpatch.log.",
                            method.FullName, e.Message);
                        Debug.Trace(e.StackTrace);
                    }
                }
            }
        }
    }
}

