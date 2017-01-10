﻿using System.Collections.Generic;
using System.Linq;
using AutoFake.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace AutoFake
{
    internal class Mocker : IMocker
    {
        private const string STATIC_CONSTRUCTOR_METHOD_NAME = ".cctor";
        private const string RET_VALUE_FLD_SUFFIX = "_RetValue";
        private const string CALLS_COUNTER_FLD_SUFFIX = "_ActualIds";
        private const string CALLBACK_FLD_SUFFIX = "_Callback";

        private readonly Mock _setup;
        private readonly MethodReference _addToListMethodInfo;
        private readonly MethodReference _invokeActionMethod;

        public Mocker(TypeInfo typeInfo, MockedMemberInfo mockedMemberInfo)
        {
            TypeInfo = typeInfo;
            MemberInfo = mockedMemberInfo;

            _setup = mockedMemberInfo.Mock;
            _addToListMethodInfo = typeInfo.Import(typeof(List<int>).GetMethod(nameof(List<int>.Add)));
            _invokeActionMethod = typeInfo.Import(typeof(Action).GetMethod(nameof(Action.Invoke)));
        }

        public TypeInfo TypeInfo { get; }
        public MockedMemberInfo MemberInfo { get; }

        public void GenerateRetValueField()
        {
            var fieldName = MemberInfo.EvaluateRetValueFieldName() + RET_VALUE_FLD_SUFFIX;
            MemberInfo.RetValueField = new FieldDefinition(fieldName, FieldAttributes.Assembly | FieldAttributes.Static,
                TypeInfo.Import(_setup.Method.ReturnType));
            TypeInfo.AddField(MemberInfo.RetValueField);
        }

        public void GenerateCallsCounter()
        {
            var fieldName = MemberInfo.EvaluateRetValueFieldName() + CALLS_COUNTER_FLD_SUFFIX;
            var collectionType = typeof(List<int>);
            MemberInfo.ActualCallsField = new FieldDefinition(fieldName, FieldAttributes.Assembly | FieldAttributes.Static,
                TypeInfo.Import(collectionType));
            TypeInfo.AddField(MemberInfo.ActualCallsField);

            var listConstructor = collectionType.GetConstructor(new Type[0]);
            var processor = GetCctorProcessor();

            processor.Append(processor.Create(OpCodes.Newobj, TypeInfo.Import(listConstructor)));
            processor.Append(processor.Create(OpCodes.Stsfld, MemberInfo.ActualCallsField));
            processor.Append(processor.Create(OpCodes.Ret));
        }

        private ILProcessor GetCctorProcessor()
        {
            var ctor = TypeInfo.Methods.SingleOrDefault(m => m.Name == STATIC_CONSTRUCTOR_METHOD_NAME);
            if (ctor == null)
            {
                ctor = AddCctor();
                return ctor.Body.GetILProcessor();
            }
            else
            {
                var processor = ctor.Body.GetILProcessor();
                processor.Remove(processor.Body.Instructions.Last()); //remove Ret
                return processor;
            }
        }

        private MethodDefinition AddCctor()
        {
            var ctor = new MethodDefinition(STATIC_CONSTRUCTOR_METHOD_NAME,
                MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName,
                TypeInfo.Import(typeof(void)));

            TypeInfo.AddMethod(ctor);
            return ctor;
        }

        public void InjectCurrentPositionSaving(ILProcessor ilProcessor, Instruction instruction)
        {
            ilProcessor.InsertAfter(instruction,
                    ilProcessor.Create(OpCodes.Callvirt, _addToListMethodInfo));
            ilProcessor.InsertAfter(instruction,
                ilProcessor.Create(OpCodes.Ldc_I4, MemberInfo.SourceCodeCallsCount));
            ilProcessor.InsertAfter(instruction,
                ilProcessor.Create(OpCodes.Ldsfld, MemberInfo.ActualCallsField));
        }

        public IList<FieldDefinition> PopMethodArguments(ILProcessor ilProcessor, Instruction instruction)
        {
            var result = new List<FieldDefinition>();
            var parametersCount = MemberInfo.Mock.SetupArguments.Count;
            var installedMethodArguments = MemberInfo.Mock.Method.GetParameters();
            for (var i = parametersCount - 1; i >= 0; i--)
            {
                var fieldName = MemberInfo.Mock.Method.Name + "Argument" + (parametersCount * MemberInfo.SourceCodeCallsCount + i);
                var field = new FieldDefinition(fieldName, FieldAttributes.Assembly | FieldAttributes.Static,
                    TypeInfo.Import(installedMethodArguments[i].ParameterType));
                TypeInfo.AddField(field);

                result.Insert(0, field);

                ilProcessor.InsertBefore(instruction, ilProcessor.Create(OpCodes.Stsfld, field));
            }

            return result;
        }

        public void RemoveMethodArguments(ILProcessor ilProcessor, Instruction instruction)
        {
            foreach (var arg in MemberInfo.Mock.SetupArguments)
                RemoveStackArgument(ilProcessor, instruction);
        }

        public void RemoveStackArgument(ILProcessor ilProcessor, Instruction instruction)
            => ilProcessor.InsertBefore(instruction, ilProcessor.Create(OpCodes.Pop));

        public void PushMethodArguments(ILProcessor ilProcessor, Instruction instruction, IEnumerable<FieldDefinition> arguments)
        {
            foreach (var field in arguments)
            {
               ilProcessor.InsertBefore(instruction, ilProcessor.Create(OpCodes.Ldsfld, field));
            }
        }

        public void RemoveInstruction(ILProcessor ilProcessor, Instruction instruction) => ilProcessor.Remove(instruction);

        public void ReplaceToRetValueField(ILProcessor ilProcessor, Instruction instruction)
            => ilProcessor.Replace(instruction,
                ilProcessor.Create(OpCodes.Ldsfld, MemberInfo.RetValueField));

        public void GenerateCallbackField()
        {
            var fieldName = MemberInfo.EvaluateRetValueFieldName() + CALLBACK_FLD_SUFFIX;
            MemberInfo.CallbackField = new FieldDefinition(fieldName, FieldAttributes.Assembly | FieldAttributes.Static,
                TypeInfo.Import(typeof(Action)));
            TypeInfo.AddField(MemberInfo.CallbackField);
        }

        public void InjectCallback(ILProcessor ilProcessor, Instruction instruction)
        {
            ilProcessor.InsertAfter(instruction,
                    ilProcessor.Create(OpCodes.Callvirt, _invokeActionMethod));
            ilProcessor.InsertAfter(instruction,
                ilProcessor.Create(OpCodes.Ldsfld, MemberInfo.CallbackField));
        }
    }
}
