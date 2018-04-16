using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal class FakeMethod
    {
        private readonly MethodDefinition _method;
        private readonly IMethodMocker _mocker;

        public FakeMethod(MethodDefinition method, IMethodMocker mocker)
        {
            _method = method;
            _mocker = mocker;
        }

        public void ApplyMock(IMock mock) => ApplyMock(_method, mock);

        private void ApplyMock(MethodDefinition currentMethod, IMock mock)
        {
            if (mock.IsAsyncMethod(currentMethod, out var asyncMethod))
            {
                ApplyMock(asyncMethod, mock);
            }

            foreach (var instruction in currentMethod.Body.Instructions.ToList())
            {
                if (mock.IsInstalledInstruction(_mocker.TypeInfo, instruction))
                {
                    var proc = currentMethod.Body.GetILProcessor();
                    mock.Inject(_mocker, proc, instruction);
                }
                else if (instruction.Operand is MethodReference method && IsFakeAssemblyMethod(method))
                {
                    var methodDefinition = method.Resolve();
                    if (methodDefinition.IsConstructor)
                    {
                        if (methodDefinition.DeclaringType.IsNestedPrivate)
                        {
                            var typeName = methodDefinition.DeclaringType.FullName.Replace("/", "+");
                            var type = _mocker.TypeInfo.SourceType.Assembly.GetType(typeName);
                            var importedType = _mocker.TypeInfo.Module.Import(type);
                            var proc = currentMethod.Body.GetILProcessor();

                            var objType = _mocker.TypeInfo.Module.Import(typeof(object));
                            var variables = new Stack<VariableDefinition>();
                            foreach (var parameter in methodDefinition.Parameters)
                            {
                                var variableDefinition = new VariableDefinition(objType);
                                if (parameter.ParameterType.IsValueType)
                                {
                                    proc.InsertBefore(instruction, Instruction.Create(OpCodes.Box, parameter.ParameterType));
                                }
                                proc.InsertBefore(instruction, Instruction.Create(OpCodes.Stloc, variableDefinition));
                                variables.Push(variableDefinition);
                                currentMethod.Body.Variables.Add(variableDefinition);
                            }

                            proc.InsertBefore(instruction, Instruction.Create(OpCodes.Ldsfld, _mocker.TypeInfo.CreateInstanceByReflectionFunc));
                            proc.InsertBefore(instruction, Instruction.Create(OpCodes.Ldtoken, importedType));
                            var getType = _mocker.TypeInfo.Module.Import(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
                            proc.InsertBefore(instruction, Instruction.Create(OpCodes.Call, getType));

                            var listType = typeof(List<object>);
                            var ctor = listType.GetConstructor(new System.Type[0]);
                            var ctorDef = _mocker.TypeInfo.Module.Import(ctor);
                            proc.InsertBefore(instruction, Instruction.Create(OpCodes.Newobj, ctorDef));
                            var listVariable = new VariableDefinition(ctorDef.DeclaringType);
                            currentMethod.Body.Variables.Add(listVariable);
                            proc.InsertBefore(instruction, Instruction.Create(OpCodes.Stloc, listVariable));
                            var addToList = _mocker.TypeInfo.Module.Import(listType.GetMethod("Add"));
                            while (variables.Count > 0)
                            {
                                proc.InsertBefore(instruction, Instruction.Create(OpCodes.Ldloc, listVariable));
                                var variableDefinition = variables.Pop();
                                proc.InsertBefore(instruction, Instruction.Create(OpCodes.Ldloc, variableDefinition));
                                proc.InsertBefore(instruction, Instruction.Create(OpCodes.Call, addToList));
                            }
                            proc.InsertBefore(instruction, Instruction.Create(OpCodes.Ldloc, listVariable));

                            var invoke = _mocker.TypeInfo.Module.Import(typeof(Func<Type, IEnumerable<object>, object>).GetMethod("Invoke"));
                            proc.Replace(instruction, Instruction.Create(OpCodes.Call, invoke));
                        }
                        else
                        {
                            instruction.Operand = _mocker.TypeInfo.ConvertToSourceAssembly(methodDefinition);
                        }
                    }
                    else
                    {
                        ApplyMock(methodDefinition, mock);
                    }
                }
            }
        }

        private bool IsFakeAssemblyMethod(MethodReference methodReference)
            => methodReference.DeclaringType.Scope is ModuleDefinition module && module == _mocker.TypeInfo.Module;
    }
}
