using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class SourceConstructor : ISourceMember
    {
        private readonly ConstructorInfo _constructor;

        public SourceConstructor(ConstructorInfo sourceConstructor)
        {
            _constructor = sourceConstructor;
            Name = sourceConstructor.Name;
            ReturnType = sourceConstructor.DeclaringType;
            HasStackInstance = false;
        }

        public string Name { get; }

        public Type ReturnType { get; }

        public bool HasStackInstance { get; }

        public bool IsCorrectInstruction(TypeInfo typeInfo, Instruction instruction)
        {
            var result = false;
            if (instruction.OpCode.OperandType == OperandType.InlineMethod)
            {
                var method = (MethodReference)instruction.Operand;
                result = method.DeclaringType.FullName == typeInfo
                    .GetMonoCecilTypeName(_constructor.DeclaringType)
                         && method.EquivalentTo(_constructor);
            }
            return result;
        }

        public ParameterInfo[] GetParameters() => _constructor.GetParameters();
    }
}
