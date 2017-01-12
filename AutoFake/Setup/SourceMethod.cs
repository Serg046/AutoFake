using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class SourceMethod : ISourceMember
    {
        private readonly MethodInfo _sourceMethod;

        public SourceMethod(MethodInfo sourceMethod)
        {
            _sourceMethod = sourceMethod;
            ReturnType = sourceMethod.ReturnType;
        }

        public string Name => _sourceMethod.Name;

        public Type ReturnType { get; }

        public bool IsCorrectInstruction(TypeInfo typeInfo, Instruction instruction)
        {
            var result = false;
            if (instruction.OpCode.OperandType == OperandType.InlineMethod)
            {
                var method = (MethodReference)instruction.Operand;
                result = method.DeclaringType.FullName == typeInfo
                    .GetInstalledMethodTypeName(_sourceMethod.DeclaringType)
                         && method.EquivalentTo(_sourceMethod);
            }
            return result;
        }

        public ParameterInfo[] GetParameters() => _sourceMethod.GetParameters();
    }
}
