using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class SourceField : ISourceMember
    {
        private static readonly OpCode[] _fieldOpCodes = {OpCodes.Ldfld, OpCodes.Ldsfld, OpCodes.Ldflda, OpCodes.Ldsflda};
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

        public bool IsSourceInstruction(ITypeInfo typeInfo, Instruction instruction)
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

        public override bool Equals(object obj)
            => obj is SourceField field && _field.Equals(field._field);

        public override int GetHashCode() => _field.GetHashCode();

        public override string ToString() => _field.ToString();
    }
}
