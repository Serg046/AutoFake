using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
	internal class SourceField : ISourceMember
	{
		private static readonly OpCode[] _fieldOpCodes = { OpCodes.Ldfld, OpCodes.Ldsfld, OpCodes.Ldflda, OpCodes.Ldsflda };
		private readonly FieldInfo _field;
		private readonly Type _fieldDeclaringType;
		private readonly SourceMember _sourceMember;
		private FieldDefinition? _monoCecilField;
		private IReadOnlyList<GenericArgument>? _genericArguments;

		public SourceField(FieldInfo field, SourceMember sourceMember)
		{
			_field = field;
			_fieldDeclaringType = field.DeclaringType ?? throw new InvalidOperationException("Declaring type must be set");
			_sourceMember = sourceMember;
			Name = field.Name;
			ReturnType = field.FieldType;
			HasStackInstance = !field.IsStatic;
		}

		public string Name { get; }

		public Type ReturnType { get; }

		public bool HasStackInstance { get; }

		public MemberInfo OriginalMember => _field;

		private FieldDefinition GetField()
			=> _monoCecilField ??= _sourceMember.TypeInfo.ImportToSourceAsm(_field).Resolve();

		public IReadOnlyList<GenericArgument> GetGenericArguments()
		{
			if (_genericArguments == null)
			{
				var genericArguments = new List<GenericArgument>();
				if (_fieldDeclaringType.IsGenericType)
				{
					var declaringType = GetField().DeclaringType.ToString();
					var types = _fieldDeclaringType.GetGenericArguments();
					var names = _fieldDeclaringType.GetGenericTypeDefinition().GetGenericArguments();
					foreach (var genericArgument in _sourceMember.GetGenericArguments(types, names, declaringType))
					{
						genericArguments.Add(genericArgument);
					}
				}

				_genericArguments = genericArguments.ToReadOnlyList();
			}

			return _genericArguments;
		}

		public bool IsSourceInstruction(Instruction instruction, IEnumerable<GenericArgument> genericArguments)
		{
			if (_fieldOpCodes.Contains(instruction.OpCode) &&
				instruction.Operand is FieldReference field &&
				field.Name == _field.Name)
			{
				var fieldDef = field.ToFieldDefinition();
				return fieldDef.ToString() == GetField().ToString() && CompareGenericArguments(fieldDef, genericArguments);
			}
			return false;
		}

		private bool CompareGenericArguments(FieldDefinition visitedField, IEnumerable<GenericArgument> genericArguments)
		{
			if (visitedField.ContainsGenericParameter)
			{
				var sourceArguments = GetGenericArguments();
				foreach (var genericParameter in visitedField.DeclaringType.GenericParameters)
				{
					if (!_sourceMember.CompareGenericArguments(genericParameter, sourceArguments, genericArguments))
					{
						return false;
					}
				}
			}

			return true;
		}

		public ParameterInfo[] GetParameters() => new ParameterInfo[0];
	}
}
