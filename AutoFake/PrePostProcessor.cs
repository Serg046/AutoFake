using AutoFake.Expression;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public FieldDefinition GenerateSetupBodyField(string name)
        {
            var type = _typeInfo.Module.Import(typeof(InvocationExpression));
            var field = new FieldDefinition(name, ACCESS_LEVEL, type);
            _typeInfo.AddField(field);
            return field;
        }

        public FieldDefinition GenerateRetValueField(string name, Type returnType)
        {
            var type = _typeInfo.Module.GetType(returnType.FullName, true)
                            ?? _typeInfo.Module.Import(returnType);
            var field = new FieldDefinition(name, ACCESS_LEVEL, type);
            _typeInfo.AddField(field);
            return field;
        }

        public FieldDefinition GenerateCallsAccumulator(string name, MethodBody method)
        {
            var type = _typeInfo.Module.Import(typeof(List<object[]>));
            var field = new FieldDefinition(name, ACCESS_LEVEL, type);
            _typeInfo.AddField(field);

            method.Instructions.Insert(0, Instruction.Create(OpCodes.Newobj,
                _typeInfo.Module.Import(typeof(List<object[]>).GetConstructor(new Type[0]))));
            method.Instructions.Insert(1, Instruction.Create(OpCodes.Stsfld, field));
            return field;
        }

        public void InjectVerification(IEmitter emitter, bool checkArguments, MethodDescriptor expectedCalls,
            FieldDefinition setupBody, FieldDefinition callsAccumulator)
        {
            var retInstruction = emitter.Body.Instructions.Last();
            emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldsfld, setupBody));
            emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldsfld, callsAccumulator));
            emitter.InsertBefore(retInstruction, Instruction.Create(checkArguments ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
            if (expectedCalls != null)
            {
                var type = _typeInfo.Module.GetType(expectedCalls.DeclaringType, true).Resolve();
                var ctor = type.Methods.Single(m => m.Name == ".ctor");
                var method = type.Methods.Single(m => m.Name == expectedCalls.Name);
                emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Newobj, ctor));
                emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldftn, method));
                var funcCtor = typeof(Func<byte, bool>).GetConstructors().Single();
                emitter.InsertBefore(retInstruction,
                    Instruction.Create(OpCodes.Newobj, _typeInfo.Module.Import(funcCtor)));
            }
            else
            {
                emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldnull));
            }
            var matchMethod = _typeInfo.Module.Import(typeof(InvocationExpression)
                .GetMethod(nameof(InvocationExpression.MatchArguments)));
            emitter.InsertBefore(retInstruction, Instruction.Create(OpCodes.Callvirt, matchMethod));
        }

        public TypeReference GetTypeReference(Type type) => _typeInfo.Module.ImportReference(type);
    }
}
