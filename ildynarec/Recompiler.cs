using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Diagnostics;

using Cecil = Mono.Cecil;
using Reflection = System.Reflection;

namespace ILDynaRec
{
    public class Recompiler
    {
        TypeResolver resolver;

        public Recompiler() {
            resolver = new TypeResolver();
        }

        public Type FindType(Cecil.TypeReference typeRef) {
            var type = resolver.FindType(typeRef);
            if (type == null) {
                throw new Exception(String.Format("Cannot resolve type {0}", typeRef));
            }

            return type;
        }

        public DynamicMethod RecompileMethod(Cecil.MethodDefinition methodDef) {
            Debug.Trace("Recompiling method: {0}", methodDef.FullName);

            var declaringType = FindType(methodDef.DeclaringType);
            var returnType = FindType(methodDef.ReturnType);
            Type[] paramTypes = methodDef.Parameters.Select((paramDef) => {
                return FindType(paramDef.ParameterType);
            }).ToArray();

            if (!methodDef.IsStatic) {
                paramTypes = new Type[] { declaringType }.Concat(paramTypes).ToArray();
            }

            var dynMethod = new DynamicMethod(methodDef.Name, returnType, paramTypes, true);
            ILGenerator il = dynMethod.GetILGenerator();
            dynMethod.InitLocals = methodDef.Body.InitLocals;
            foreach (var variable in methodDef.Body.Variables) {
                var localType = FindType(variable.VariableType);
                Debug.Trace("Declaring local (cecil type: {0}) of type (runtime type: {1})", variable.VariableType, localType);
                il.DeclareLocal(localType);
            }

            var labels = new Dictionary<Cecil.Cil.Instruction, Label>();

            foreach (var inst in methodDef.Body.Instructions) {
                if (inst.Operand != null && inst.Operand.GetType() == typeof(Cecil.Cil.Instruction)) {
                    var opinst = (Cecil.Cil.Instruction)(inst.Operand);
                    labels[opinst] = il.DefineLabel();
                }
            }

            foreach (var inst in Instrument.IterateInstructions(methodDef)) {
                Debug.Trace("Emitting: {0}", inst);

                Label label;
                if (labels.TryGetValue(inst, out label)) {
                    il.MarkLabel(label);
                }

                var ilop = FindOpcode(inst.OpCode);

                var bflags_all = Reflection.BindingFlags.NonPublic |
                                 Reflection.BindingFlags.Instance |
                                 Reflection.BindingFlags.Static |
                                 Reflection.BindingFlags.Public |
                                 Reflection.BindingFlags.FlattenHierarchy;

                if (inst.Operand != null) {
                    var operand = inst.Operand;
                    var operandType = operand.GetType();
                    if (operandType == typeof(Cecil.Cil.Instruction)) {
                        //branch location     
                        var operandInst = (Cecil.Cil.Instruction)operand;

                        il.Emit(ilop, labels[operandInst]);
                    }
                    else if (operandType == typeof(sbyte)) {
                        il.Emit(ilop, (sbyte)operand);
                    }
                    else if (operandType == typeof(Int16)) {
                        il.Emit(ilop, (Int16)operand);
                    }
                    else if (operandType == typeof(Int32)) {
                        il.Emit(ilop, (Int32)operand);
                    }
                    else if (operandType == typeof(Single)) {
                        il.Emit(ilop, (Single)operand);
                    }
                    else if (operandType == typeof(Double)) {
                        il.Emit(ilop, (Double)operand);
                    }
                    else if (operandType == typeof(Cecil.MethodReference) ||
                        operandType == typeof(Cecil.MethodDefinition)) {
                        var operandMethod = (Cecil.MethodReference)operand;
                        var ourType = FindType(operandMethod.DeclaringType);
                        Type[] opParamTypes = operandMethod.Parameters.Select((prm) => {
                            if (prm.ParameterType is Cecil.GenericParameter) {
                                throw new Exception("Unsupported generic parameter");
                                //return ourType.GenericTypeArguments[((Cecil.GenericParameter)prm.ParameterType).Position];
                            }

                            return FindType(prm.ParameterType);
                        }).ToArray();

                        if (operandMethod.Name == ".ctor") {
                            il.Emit(ilop, ourType.GetConstructor(
                                Reflection.BindingFlags.Instance |
                                Reflection.BindingFlags.Static /*| Reflection.BindingFlags.NonPublic */| Reflection.BindingFlags.Public,
                                binder: null,
                                types: opParamTypes,
                                modifiers: null));
                        }
                        else {
                            il.Emit(ilop, ourType.GetMethod(operandMethod.Name,
                                bindingAttr: bflags_all,
                                binder: null,
                                types: opParamTypes,
                                modifiers: null));
                        }
                    }
                    else if (operandType == typeof(Cecil.FieldDefinition) ||
                        operandType == typeof(Cecil.FieldReference)) {
                        var operandField = (Cecil.FieldReference)operand;
                        var ourType = FindType(operandField.DeclaringType);

                        il.Emit(ilop, ourType.GetField(operandField.Name, bindingAttr: bflags_all));
                    }
                    else if (operandType == typeof(Cecil.GenericInstanceMethod)) {
                        var operandGeneric = (Cecil.GenericInstanceMethod)operand;
                        var ourType = FindType(operandGeneric.DeclaringType);

                        Type[] opGenericArgs = operandGeneric.GenericArguments.Select((prm) => {
                            return FindType(prm);
                        }).ToArray();

                        Type[] opParamTypes = operandGeneric.Parameters.Select((prm) => {
                            if (prm.ParameterType is Cecil.GenericParameter) {
                                var genericParam = ((Cecil.GenericParameter)prm.ParameterType);
                                return opGenericArgs[genericParam.Position];
                            }

                            return FindType(prm.ParameterType);
                        }).ToArray();

                        var methods = ourType.GetMethods(bindingAttr: bflags_all);

                        var ourMethod = methods.First(met => {
                            return met.Name == operandGeneric.Name &&
                                met.GetGenericArguments().Length == opGenericArgs.Length &&
                                met.GetParameters().Length == opParamTypes.Length;
                        });

                        var metParams = ourMethod.GetParameters();

                        ourMethod = ourMethod.MakeGenericMethod(opGenericArgs);
                        il.Emit(ilop, ourMethod);
                    }
                    else if (operandType == typeof(string)) {
                        il.Emit(ilop, (string)operand);
                    }
                    else if (operandType == typeof(Cecil.TypeReference)) {
                        il.Emit(ilop, FindType((Cecil.TypeReference)operand));
                    }
                    else if (operandType == typeof(Cecil.Cil.VariableDefinition)) {
                        var vardef = (Cecil.Cil.VariableDefinition)operand;
                        il.Emit(ilop, vardef.Index);
                    }
                    else {
                        Debug.Trace("Unexpected operand {0}", operandType);
                        Debug.LogErrorFormat("Unexpected operand {0}", operandType);
                        return null;
                    }
                }
                else {
                    il.Emit(ilop);
                }

                //Console.WriteLine("Emitted {0}", il.ILOffset);

            }

            return dynMethod;
        }

        public static OpCode FindOpcode(Cecil.Cil.OpCode cop) {
            var opType = typeof(OpCodes).GetFields().First((op) => {
                return op.Name.ToLower().Replace('_', '.') == cop.Name;
            });

            return (OpCode)opType.GetValue(null);
        }
    }
}