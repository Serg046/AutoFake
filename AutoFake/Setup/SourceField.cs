using System;
using System.Collections.Generic;
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
        private FieldDefinition? _monoCecilField;
        private IList<GenericArgument>? _genericArguments;

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

        public MemberInfo OriginalMember => _field;

        private FieldDefinition GetField(ITypeInfo typeInfo)
	        => _monoCecilField ??= typeInfo.ImportReference(_field).Resolve();

        public IList<GenericArgument> GetGenericArguments(ITypeInfo typeInfo)
        {
	        if (_genericArguments == null)
	        {
		        _genericArguments = new List<GenericArgument>();
		        if (_field.DeclaringType.IsGenericType)
		        {
					var declaringType = GetField(typeInfo).DeclaringType.ToString();
			        var types = _field.DeclaringType.GetGenericArguments();
			        var names = _field.DeclaringType.GetGenericTypeDefinition().GetGenericArguments();
			        for (int i = 0; i < types.Length; i++)
			        {
				        var typeRef = typeInfo.ImportReference(types[i]);
				        _genericArguments.Add(new GenericArgument(names[i].ToString(), typeRef.ToString(), declaringType));
			        }
		        }
	        }

	        return _genericArguments;
        }

        public bool IsSourceInstruction(ITypeInfo typeInfo, Instruction instruction, IEnumerable<GenericArgument> genericArguments)
        {
            if (_fieldOpCodes.Contains(instruction.OpCode) &&
                instruction.Operand is FieldReference field &&
                field.Name == _field.Name)
            {
	            var fieldDef = field.ToFieldDefinition();
	            return fieldDef.ToString() == GetField(typeInfo).ToString() &&
					(!field.ContainsGenericParameter || CompareGenericArguments(fieldDef, genericArguments, typeInfo));
            }
            return false;
        }

        private bool CompareGenericArguments(FieldDefinition visitedField, IEnumerable<GenericArgument> genericArguments, ITypeInfo typeInfo)
        {
	        if (visitedField.ContainsGenericParameter)
	        {
		        var arguments = GetGenericArguments(typeInfo);
				foreach (var genericParameter in visitedField.DeclaringType.GenericParameters)
				{
					var source = arguments.SingleOrDefault(a => a.Name == genericParameter.Name);
					var visited = genericArguments.FindGenericTypeOrDefault(genericParameter.Name);
					if (source == null || visited == null || source.Type != visited.Type)
					{
						return false;
					}
				}
            }

            return true;
        }

        public ParameterInfo[] GetParameters() => new ParameterInfo[0];

        public override bool Equals(object obj)
            => obj is SourceField field && _field.Equals(field._field);

        public override int GetHashCode() => _field.GetHashCode();

        public override string ToString() => _field.ToString();
    }
}
