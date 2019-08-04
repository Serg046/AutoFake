using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using AutoFake.Expression;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace AutoFake
{
    internal class Mocker : IMocker
    {
        private readonly MethodReference _addToListOfObjArray;
        private readonly MethodReference _matchArgumentsMethod;

        public Mocker(ITypeInfo typeInfo, MockedMemberInfo mockedMemberInfo)
        {
            TypeInfo = typeInfo;
            MemberInfo = mockedMemberInfo;

            _addToListOfObjArray = typeInfo.Module.Import(typeof(List<object[]>).GetMethod(nameof(List<object[]>.Add)));
            _matchArgumentsMethod = TypeInfo.Module.Import(typeof(InvocationExpression).GetMethod(nameof(InvocationExpression.MatchArguments)));
        }

        public ITypeInfo TypeInfo { get; }
        public MockedMemberInfo MemberInfo { get; }

        public void GenerateSetupBodyField()
        {
            var fieldName = MemberInfo.UniqueName + "_SetupBody";
            var fieldType = TypeInfo.Module.Import(typeof(InvocationExpression));
            MemberInfo.SetupBodyField = new FieldDefinition(fieldName, FieldAttributes.Assembly | FieldAttributes.Static, fieldType);
            TypeInfo.AddField(MemberInfo.SetupBodyField);
        }

        public void GenerateRetValueField(Type returnType)
        {
            var fieldName = MemberInfo.UniqueName + "_RetValue";
            var fieldType = TypeInfo.Module.GetType(TypeInfo.GetMonoCecilTypeName(returnType))
                ?? TypeInfo.Module.Import(returnType);
            MemberInfo.RetValueField = new FieldDefinition(fieldName, FieldAttributes.Assembly | FieldAttributes.Static, fieldType);
            TypeInfo.AddField(MemberInfo.RetValueField);
        }

        public void GenerateCallsCounterFuncField()
        {
            var fieldName = MemberInfo.UniqueName + "_ExpectedCallsFunc";
            MemberInfo.ExpectedCallsFuncField = new FieldDefinition(fieldName, FieldAttributes.Assembly | FieldAttributes.Static,
                TypeInfo.Module.Import(typeof(Func<byte, bool>)));
            TypeInfo.AddField(MemberInfo.ExpectedCallsFuncField);
        }

        public IList<VariableDefinition> SaveMethodCall(ILProcessor ilProcessor, Instruction instruction, bool checkArguments)
        {
            if (MemberInfo.ActualCallsAccumulator == null)
            {
                MemberInfo.ActualCallsAccumulator = new VariableDefinition(TypeInfo.Module.Import(typeof(List<object[]>)));
                ilProcessor.Body.Variables.Add(MemberInfo.ActualCallsAccumulator);
                ilProcessor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Newobj,
                    TypeInfo.Module.Import(typeof(List<object[]>).GetConstructor(new Type[0]))));
                ilProcessor.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Stloc, MemberInfo.ActualCallsAccumulator));
            }

            var method = (MethodReference)instruction.Operand;
            var variables = new Stack<VariableDefinition>();
            foreach (var parameter in method.Parameters.Reverse())
            {
                var variable = new VariableDefinition(parameter.ParameterType);
                variables.Push(variable);
                ilProcessor.Body.Variables.Add(variable);
                ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Stloc, variable));
            }
            var objRef = TypeInfo.Module.Import(typeof(object));
            ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldc_I4, variables.Count));
            ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Newarr, objRef));
            var arrVar = new VariableDefinition(TypeInfo.Module.Import(typeof(object[])));
            ilProcessor.Body.Variables.Add(arrVar);
            ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Stloc, arrVar));

            if (checkArguments)
            {
                var counter = 0;
                foreach (var variable in variables)
                {
                    ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldloc, arrVar));
                    ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldc_I4, counter++));
                    ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldloc, variable));
                    if (variable.VariableType.IsValueType)
                    {
                        ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Box, variable.VariableType));
                    }
                    ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Stelem_Ref));
                }
            }

            ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldloc, MemberInfo.ActualCallsAccumulator));
            ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldloc, arrVar));
            ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Call, _addToListOfObjArray));

            return variables.ToList();
        }

        public void RemoveMethodArgumentsIfAny(ILProcessor ilProcessor, Instruction instruction)
        {
            if (instruction.Operand is MethodReference method)
            {
                for (var i = 0; i < method.Parameters.Count; i++)
                {
                    RemoveStackArgument(ilProcessor, instruction);
                }
            }
        }

        public void RemoveStackArgument(ILProcessor ilProcessor, Instruction instruction)
            => ilProcessor.InsertBefore(instruction, ilProcessor.Create(OpCodes.Pop));

        public void PushMethodArguments(ILProcessor ilProcessor, Instruction instruction, IEnumerable<VariableDefinition> variables)
        {
            foreach (var variable in variables)
            {
                ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldloc, variable));
            }
        }

        public void RemoveInstruction(ILProcessor ilProcessor, Instruction instruction) => ilProcessor.Remove(instruction);

        public void ReplaceToRetValueField(ILProcessor ilProcessor, Instruction instruction)
            => ilProcessor.Replace(instruction,
                ilProcessor.Create(OpCodes.Ldsfld, MemberInfo.RetValueField));
        
        public void InjectCallback(ILProcessor ilProcessor, Instruction instruction, MethodDescriptor callback)
        {
            var type = TypeInfo.Module.GetType(callback.DeclaringType, true).Resolve();
            var ctor = type.Methods.Single(m => m.Name == ".ctor");
            var method = type.Methods.Single(m => m.Name == callback.Name);
            ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Newobj, ctor));
            ilProcessor.InsertBefore(instruction, Instruction.Create(OpCodes.Call, method));
        }

        public void InjectVerification(ILProcessor ilProcessor, bool checkArguments, bool expectedCalls)
        {
            var retInstruction = ilProcessor.Body.Instructions.Last();
            ilProcessor.InsertBefore(retInstruction, ilProcessor.Create(OpCodes.Ldsfld, MemberInfo.SetupBodyField));
            ilProcessor.InsertBefore(retInstruction, Instruction.Create(OpCodes.Ldloc, MemberInfo.ActualCallsAccumulator));
            ilProcessor.InsertBefore(retInstruction, ilProcessor.Create(checkArguments ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
            if (expectedCalls)
            {
                ilProcessor.InsertBefore(retInstruction, ilProcessor.Create(OpCodes.Ldsfld, MemberInfo.ExpectedCallsFuncField));
            }
            else
            {
                ilProcessor.InsertBefore(retInstruction, ilProcessor.Create(OpCodes.Ldnull));
            }
            ilProcessor.InsertBefore(retInstruction, ilProcessor.Create(OpCodes.Callvirt, _matchArgumentsMethod));
        }
    }
}
