using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Cil;

namespace AutoFake.Abstractions.Setup
{
	internal interface ISourceMember
	{
		string Name { get; }
		Type ReturnType { get; }
		MemberInfo OriginalMember { get; }
		IReadOnlyList<GenericArgument> GetGenericArguments();
		bool IsSourceInstruction(Instruction instruction, IEnumerable<GenericArgument> genericArguments);
		ParameterInfo[] GetParameters();
		bool HasStackInstance { get; }
	}
}
