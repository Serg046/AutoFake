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
	internal class SourceMethod : SourceMember, ISourceMember
	{
		private readonly MethodBase _method;
		private MethodDefinition? _monoCecilMethodDef;
		private IReadOnlyList<IGenericArgument>? _genericArguments;

		public SourceMethod(ITypeInfo typeInfo, IGenericArgument.Create createGenericArgument, MethodInfo method)
			: base(typeInfo, createGenericArgument, method)
		{
			_method = method;
			ReturnType = method.ReturnType;
			HasStackInstance = !method.IsStatic;
		}

		public SourceMethod(ITypeInfo typeInfo, IGenericArgument.Create createGenericArgument, ConstructorInfo ctor)
			: base(typeInfo, createGenericArgument, ctor)
		{
			_method = ctor;
			ReturnType = DeclaringType;
			HasStackInstance = false;
		}

		public Type ReturnType { get; }

		public bool HasStackInstance { get; }

		public MemberInfo OriginalMember => _method;

		public MethodDefinition GetMethod()
			=> _monoCecilMethodDef ??= TypeInfo.ImportToSourceAsm(_method).Resolve();

		public IReadOnlyList<IGenericArgument> GetGenericArguments()
		{
			return _genericArguments ??= GetGenericArgumentsImpl().ToReadOnlyList();
		}

		private IEnumerable<IGenericArgument> GetGenericArgumentsImpl()
		{
			var declaringType = GetMethod().DeclaringType.ToString();
			if (DeclaringType.IsGenericType)
			{
				foreach (var genericArgument in GetGenericArguments(DeclaringType, declaringType))
				{
					yield return genericArgument;
				}
			}

			if (_method.IsGenericMethod && _method is MethodInfo method)
			{
				foreach (var genericArgument in GetGenericArguments(method, declaringType))
				{
					yield return genericArgument;
				}
			}
		}

		public bool IsSourceInstruction(Instruction instruction, IEnumerable<IGenericArgument> genericArguments)
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

		private bool CompareGenericArguments(MethodDefinition visitedMethod, IEnumerable<IGenericArgument> genericArguments)
		{
			if (visitedMethod.HasGenericParameters || visitedMethod.DeclaringType.HasGenericParameters)
			{
				var sourceArguments = GetGenericArguments();
				foreach (var genericParameter in visitedMethod.GenericParameters.Concat(visitedMethod.DeclaringType.GenericParameters))
				{
					if (!CompareGenericArguments(genericParameter, sourceArguments, genericArguments))
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
