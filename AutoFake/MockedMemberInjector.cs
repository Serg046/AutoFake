﻿using System.Collections.Generic;
using GuardExtensions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal class MockedMemberInjector
    {
        private readonly TypeInfo _typeInfo;
        private readonly ILProcessor _ilProcessor;
        private readonly MockedMemberInfo _mockedMemberInfo;

        public MockedMemberInjector(TypeInfo typeInfo, ILProcessor processor, MockedMemberInfo mockedMemberInfo)
        {
            _typeInfo = typeInfo;
            Guard.AreNotNull(processor, mockedMemberInfo);

            _ilProcessor = processor;
            _mockedMemberInfo = mockedMemberInfo;
        }

        public void Process(Instruction instruction)
        {
            var methodReference = (MethodReference)instruction.Operand;

            ProcessMethodArguments(instruction);

            if (!methodReference.Resolve().IsStatic)
                _ilProcessor.InsertBefore(instruction, _ilProcessor.Create(OpCodes.Pop));

            MarkCurrentMethodPosition(instruction);

            if (_mockedMemberInfo.Setup.IsVoid)
                _ilProcessor.Remove(instruction);
            else
                _ilProcessor.Replace(instruction, _ilProcessor.Create(OpCodes.Ldsfld, _mockedMemberInfo.ReturnValueField));

            _mockedMemberInfo.SourceCodeCallsCount++;
        }

        private void ProcessMethodArguments(Instruction instruction)
        {
            var parametersCount = _mockedMemberInfo.Setup.SetupArguments.Length;
            if (parametersCount > 0)
            {
                if (_mockedMemberInfo.Setup.IsVerifiable)
                {
                    ExtractMethodArguments(instruction);
                }
                else
                {
                    for (var i = 0; i < parametersCount; i++)
                    {
                        _ilProcessor.InsertBefore(instruction, _ilProcessor.Create(OpCodes.Pop));
                    }
                }
            }
        }

        private void ExtractMethodArguments(Instruction instruction)
        {
            var parametersCount = _mockedMemberInfo.Setup.SetupArguments.Length;
            var argumentFields = new List<FieldDefinition>();

            for (var i = 0; i < parametersCount; i++)
            {
                var idx = parametersCount - i - 1;
                var currentArg = _mockedMemberInfo.Setup.SetupArguments[idx];
                var fieldName = _mockedMemberInfo.Setup.Method.Name + "Argument" +
                                (parametersCount * _mockedMemberInfo.SourceCodeCallsCount + idx).ToString();
                var field = new FieldDefinition(fieldName, FieldAttributes.Assembly | FieldAttributes.Static,
                    _typeInfo.Import(currentArg.GetType()));
                _typeInfo.AddField(field);

                argumentFields.Insert(0, field);
                _ilProcessor.InsertBefore(instruction, _ilProcessor.Create(OpCodes.Stsfld, field));
            }

            _mockedMemberInfo.AddArguments(argumentFields);
        }

        private void MarkCurrentMethodPosition(Instruction instruction)
        {
            if (_mockedMemberInfo.Setup.IsVerifiable || _mockedMemberInfo.Setup.ExpectedCallsCount != -1)
            {
                _ilProcessor.InsertBefore(instruction,
                    _ilProcessor.Create(OpCodes.Ldsfld, _mockedMemberInfo.ActualCallsIdsField));
                _ilProcessor.InsertBefore(instruction,
                    _ilProcessor.Create(OpCodes.Ldc_I4, _mockedMemberInfo.SourceCodeCallsCount));
                _ilProcessor.InsertBefore(instruction,
                    _ilProcessor.Create(OpCodes.Callvirt, _typeInfo.AddToListMethodInfo));
            }
        }
    }
}
