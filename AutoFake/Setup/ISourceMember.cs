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
        IList<GenericArgument> GetGenericArguments(ITypeInfo typeInfo);
        bool IsSourceInstruction(ITypeInfo typeInfo, Instruction instruction, IEnumerable<GenericArgument> genericArguments);
        ParameterInfo[] GetParameters();
        bool HasStackInstance { get; }
    }
}
