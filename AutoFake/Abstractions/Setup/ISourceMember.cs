using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Cil;

namespace AutoFake.Abstractions.Setup;

public interface ISourceMember
{
	string Name { get; }
	Type ReturnType { get; }
	MemberInfo OriginalMember { get; }
	IReadOnlyList<IGenericArgument> GetGenericArguments();
	bool IsSourceInstruction(Instruction instruction, IEnumerable<IGenericArgument> genericArguments);
	ParameterInfo[] GetParameters();
	bool HasStackInstance { get; }
}
