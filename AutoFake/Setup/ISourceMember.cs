using Mono.Cecil.Cil;
using System;
using System.Reflection;

namespace AutoFake.Setup
{
    internal interface ISourceMember
    {
        string Name { get; }
        Type ReturnType { get; }
        bool IsCorrectInstruction(TypeInfo typeInfo, Instruction instruction);
        ParameterInfo[] GetParameters();
    }
}
