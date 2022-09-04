using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Abstractions.Setup;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
	internal class SourceMethod : ISourceMember
	{
		private readonly MethodBase _method;
		private readonly Type _methodDeclaringType;
		private readonly SourceMember _sourceMember;
		private MethodDefinition? _monoCecilMethodDef;
		private IReadOnlyList<GenericArgument>? _genericArguments;

		public SourceMethod(MethodInfo sourceMethod, SourceMember sourceMember)
		{
			_method = sourceMethod;
			_methodDeclaringType = sourceMethod.DeclaringType ?? throw new InvalidOperationException("Declaring type must be set");
			_sourceMember = sourceMember;
			Name = sourceMethod.Name;
			ReturnType = sourceMethod.ReturnType;
			HasStackInstance = !sourceMethod.IsStatic;
		}

		public SourceMethod(ConstructorInfo sourceMethod, SourceMember sourceMember)
		{
			_method = sourceMethod;
			_sourceMember = sourceMember;
			Name = sourceMethod.Name;
			_methodDeclaringType = ReturnType = sourceMethod.DeclaringType ?? throw new InvalidOperationException("Declaring type must be set");
			HasStackInstance = false;
		}

		public string Name { get; }

		public Type ReturnType { get; }

		public bool HasStackInstance { get; }

		public MemberInfo OriginalMember => _method;

		public MethodDefinition GetMethod()
			=> _monoCecilMethodDef ??= _sourceMember.TypeInfo.ImportToSourceAsm(_method).Resolve();

		public IReadOnlyList<GenericArgument> GetGenericArguments()
		{
			return _genericArguments ??= GetGenericArgumentsImpl().ToReadOnlyList();
		}

		private IEnumerable<GenericArgument> GetGenericArgumentsImpl()
		{
			var declaringType = GetMethod().DeclaringType.ToString();
			if (_methodDeclaringType.IsGenericType)
			{
				var types = _methodDeclaringType.GetGenericArguments();
				var names = _methodDeclaringType.GetGenericTypeDefinition().GetGenericArguments();
				foreach (var genericArgument in _sourceMember.GetGenericArguments(types, names, declaringType))
				{
					yield return genericArgument;
				}
			}

			if (_method.IsGenericMethod && _method is MethodInfo method)
			{
				var types = method.GetGenericArguments();
				var names = method.GetGenericMethodDefinition().GetGenericArguments();
				foreach (var genericArgument in _sourceMember.GetGenericArguments(types, names, declaringType))
				{
					yield return genericArgument;
				}
			}
		}

		public bool IsSourceInstruction(Instruction instruction, IEnumerable<GenericArgument> genericArguments)
		{
			if (instruction.OpCode.OperandType == OperandType.InlineMethod &&
				instruction.Operand is MethodReference method &&
				method.Name == _method.Name)
			{
				var methodDef = method.ToMethodDefinition();
				return methodDef.ToString() == GetMethod().ToString() && CompareGenericArguments(methodDef, genericArguments);
			}
			return false;
		}

		private bool CompareGenericArguments(MethodDefinition visitedMethod, IEnumerable<GenericArgument> genericArguments)
		{
			if (visitedMethod.HasGenericParameters || visitedMethod.DeclaringType.HasGenericParameters)
			{
				var sourceArguments = GetGenericArguments();
				foreach (var genericParameter in visitedMethod.GenericParameters.Concat(visitedMethod.DeclaringType.GenericParameters))
				{
					if (!_sourceMember.CompareGenericArguments(genericParameter, sourceArguments, genericArguments))
					{
						return false;
					}
				}
			}

			return true;
		}

		public ParameterInfo[] GetParameters() => _method.GetParameters();
	}
}
