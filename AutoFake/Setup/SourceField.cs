using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class SourceField : ISourceMember
    {
        private static readonly OpCode[] _fieldOpCodes = {OpCodes.Ldfld, OpCodes.Ldsfld};
        private readonly FieldInfo _field;

        public SourceField(FieldInfo field)
        {
            _field = field;
            Name = field.Name;
            ReturnType = field.FieldType;
            HasStackInstance = !field.IsStatic;
        }

        public string Name { get; }

        public Type ReturnType { get; }

        public bool HasStackInstance { get; }

        public bool IsCorrectInstruction(TypeInfo typeInfo, Instruction instruction)
        {
            var result = false;
            if (_fieldOpCodes.Contains(instruction.OpCode))
            {
                var field = (FieldReference)instruction.Operand;
                result = field.Name == _field.Name &&
                         field.DeclaringType.FullName == typeInfo.GetMonoCecilTypeName(_field.DeclaringType);
            }
            return result;
        }

        public ParameterInfo[] GetParameters() => new ParameterInfo[0];

    }
}
