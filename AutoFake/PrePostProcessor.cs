using AutoFake.Expression;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace AutoFake
{
    internal class PrePostProcessor : IPrePostProcessor
    {
        private const FieldAttributes ACCESS_LEVEL = FieldAttributes.Assembly | FieldAttributes.Static;
        private readonly ITypeInfo _typeInfo;

        public PrePostProcessor(ITypeInfo typeInfo)
        {
            _typeInfo = typeInfo;
        }

        public FieldDefinition GenerateField(string name, Type returnType)
        {
            var type = _typeInfo.Module.ImportReference(returnType);
            var field = new FieldDefinition(name, ACCESS_LEVEL, type);
            _typeInfo.AddField(field);
            return field;
        }

        public FieldDefinition GenerateCallsAccumulator(string name, MethodBody method)
        {
            var type = _typeInfo.Module.ImportReference(typeof(List<object[]>));
            var field = new FieldDefinition(name, ACCESS_LEVEL, type);
            _typeInfo.AddField(field);

            method.Instructions.Insert(0, Instruction.Create(OpCodes.Newobj,
                _typeInfo.Module.ImportReference(typeof(List<object[]>).GetConstructor(new Type[0]))));
            method.Instructions.Insert(1, Instruction.Create(OpCodes.Stsfld, field));
            return field;
        }

        public void InjectVerification(IEmitter emitter, bool checkArguments, FieldDefinition expectedCalls,
            FieldDefinition setupBody, FieldDefinition callsAccumulator)
        {
            var retInstruction = emitter.Body.Instructions.Last();

            var argMatcher = GetArgumentsMatcher(emitter.Body.Method, out var isAsync);
            VariableDefinition retValue = null;
            if (isAsync)
            {
                retValue = new VariableDefinition(emitter.Body.Method.ReturnType);
                emitter.Body.Variables.Add(retValue);
                emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Stloc, retValue));
            }

            emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldsfld, setupBody));
            if (retValue != null) emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldloc, retValue));
            emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldsfld, callsAccumulator));
            emitter.InsertBefore(retInstruction, Instruction.Create(checkArguments ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
            if (expectedCalls != null)
            {
                emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldsfld, expectedCalls));
            }
            else
            {
                emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldnull));
            }
            emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Call, argMatcher));
        }

        private MethodReference GetArgumentsMatcher(MethodReference method, out bool isAsync)
        {
            var returnType = method.ReturnType;
            if (returnType.FullName == typeof(Task).FullName)
            {
                isAsync = true;
                var methodInfo = typeof(InvocationExpression).GetMethod(nameof(InvocationExpression.MatchArgumentsAsync));
                return _typeInfo.Module.ImportReference(methodInfo);
            }
            else if (returnType.Namespace == typeof(Task).Namespace && returnType.Name == "Task`1" &&
                     returnType is GenericInstanceType genericReturnType)
            {
                isAsync = true;
                var methodInfo = typeof(InvocationExpression).GetMethod(nameof(InvocationExpression.MatchArgumentsGenericAsync));
                var open = _typeInfo.Module.ImportReference(methodInfo);
                var closed = new GenericInstanceMethod(open);
                closed.GenericArguments.Add(genericReturnType.GenericArguments.Single());
                return closed;
            }
            else
            {
                isAsync = false;
                var methodInfo = typeof(InvocationExpression).GetMethod(nameof(InvocationExpression.MatchArguments));
                return _typeInfo.Module.ImportReference(methodInfo);
            }
        }
    }
}
