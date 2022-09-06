using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
	internal class SourceField : SourceMember, ISourceMember
	{
		private static readonly OpCode[] _fieldOpCodes = { OpCodes.Ldfld, OpCodes.Ldsfld, OpCodes.Ldflda, OpCodes.Ldsflda };
		private readonly FieldInfo _field;
		private FieldDefinition? _monoCecilField;
		private IReadOnlyList<GenericArgument>? _genericArguments;

		public SourceField(ITypeInfo typeInfo, GenericArgument.Create createGenericArgument, FieldInfo field)
			: base(typeInfo, createGenericArgument, field)
		{
			_field = field;
			ReturnType = field.FieldType;
			HasStackInstance = !field.IsStatic;
		}

		public Type ReturnType { get; }

		public bool HasStackInstance { get; }

		public MemberInfo OriginalMember => _field;

		private FieldDefinition GetField()
			=> _monoCecilField ??= TypeInfo.ImportToSourceAsm(_field).Resolve();

		public IReadOnlyList<GenericArgument> GetGenericArguments()
		{
			if (_genericArguments == null)
			{
				var genericArguments = new List<GenericArgument>();
				if (DeclaringType.IsGenericType)
				{
					var declaringType = GetField().DeclaringType.ToString();
					var types = DeclaringType.GetGenericArguments();
					var names = DeclaringType.GetGenericTypeDefinition().GetGenericArguments();
					foreach (var genericArgument in GetGenericArguments(types, names, declaringType))
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
					if (!CompareGenericArguments(genericParameter, sourceArguments, genericArguments))
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
