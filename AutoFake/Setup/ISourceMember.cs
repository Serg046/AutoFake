using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AutoFake.Setup
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
