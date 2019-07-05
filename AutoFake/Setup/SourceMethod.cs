using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class SourceMethod : ISourceMember
    {
        private readonly MethodInfo _method;

        public SourceMethod(MethodInfo sourceMethod)
        {
            _method = sourceMethod;
            Name = sourceMethod.Name;
            ReturnType = sourceMethod.ReturnType;
            HasStackInstance = !sourceMethod.IsStatic;
        }

        public string Name { get; }

        public Type ReturnType { get; }

        public bool HasStackInstance { get; }

        public bool IsCorrectInstruction(ITypeInfo typeInfo, Instruction instruction)
        {
            var result = false;
            if (instruction.OpCode.OperandType == OperandType.InlineMethod)
            {
                var method = (MethodReference)instruction.Operand;
                result = method.DeclaringType.FullName == typeInfo
                    .GetMonoCecilTypeName(_method.DeclaringType)
                         && method.EquivalentTo(_method);
            }
            return result;
        }

        public ParameterInfo[] GetParameters() => _method.GetParameters();

        public override bool Equals(object obj)
            => obj is SourceMethod method && _method.Equals(method._method);

        public override int GetHashCode() => _method.GetHashCode();

        public override string ToString() => _method.ToString();
    }
}
