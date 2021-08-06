using Mono.Cecil.Cil;
using System;
using System.Reflection;

namespace AutoFake.Setup
{
    internal interface ISourceMember
    {
        string Name { get; }
        Type ReturnType { get; }
        MemberInfo OriginalMember { get; }
        bool IsSourceInstruction(ITypeInfo typeInfo, Instruction instruction);
        ParameterInfo[] GetParameters();
        bool HasStackInstance { get; }
    }
}
