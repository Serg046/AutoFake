﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class SourceField : SourceMember, ISourceMember
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

        private FieldDefinition GetField(IAssemblyWriter assemblyWriter)
	        => _monoCecilField ??= assemblyWriter.ImportToSourceAsm(_field).Resolve();

        public IList<GenericArgument> GetGenericArguments(IAssemblyWriter assemblyWriter)
        {
	        if (_genericArguments == null)
	        {
		        _genericArguments = new List<GenericArgument>();
		        if (_field.DeclaringType?.IsGenericType == true)
		        {
					var declaringType = GetField(assemblyWriter).DeclaringType.ToString();
			        var types = _field.DeclaringType.GetGenericArguments();
			        var names = _field.DeclaringType.GetGenericTypeDefinition().GetGenericArguments();
			        foreach (var genericArgument in GetGenericArguments(assemblyWriter, types, names, declaringType))
			        {
				        _genericArguments.Add(genericArgument);
			        }
		        }
	        }

	        return _genericArguments;
        }

        public bool IsSourceInstruction(IAssemblyWriter assemblyWriter, Instruction instruction, IEnumerable<GenericArgument> genericArguments)
        {
            if (_fieldOpCodes.Contains(instruction.OpCode) &&
                instruction.Operand is FieldReference field &&
                field.Name == _field.Name)
            {
	            var fieldDef = field.ToFieldDefinition();
	            return fieldDef.ToString() == GetField(assemblyWriter).ToString() && CompareGenericArguments(fieldDef, genericArguments, assemblyWriter);
            }
            return false;
        }

        private bool CompareGenericArguments(FieldDefinition visitedField, IEnumerable<GenericArgument> genericArguments, IAssemblyWriter assemblyWriter)
        {
	        if (visitedField.ContainsGenericParameter)
	        {
		        var sourceArguments = GetGenericArguments(assemblyWriter);
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

        public override bool Equals(object? obj)
            => obj is SourceField field && _field.Equals(field._field);

        public override int GetHashCode() => _field.GetHashCode();

        public override string? ToString() => _field.ToString();
    }
}
