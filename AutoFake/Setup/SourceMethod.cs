using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class SourceMethod : ISourceMember
    {
        private readonly MethodBase _method;
        private MethodDefinition _monoCecilMethod;

        public SourceMethod(MethodInfo sourceMethod)
        {
            _method = sourceMethod;
            Name = sourceMethod.Name;
            ReturnType = sourceMethod.ReturnType;
            HasStackInstance = !sourceMethod.IsStatic;
        }

        public SourceMethod(ConstructorInfo sourceMethod)
        {
            _method = sourceMethod;
            Name = sourceMethod.Name;
            ReturnType = sourceMethod.DeclaringType;
            HasStackInstance = false;
        }

        public string Name { get; }

        public Type ReturnType { get; }

        public bool HasStackInstance { get; }

        public MemberInfo OriginalMember => _method;

        public bool IsSourceInstruction(ITypeInfo typeInfo, Instruction instruction)
        {
            if (instruction.OpCode.OperandType == OperandType.InlineMethod &&
                instruction.Operand is MethodReference method &&
                method.Name == _method.Name)
            {
                if (_monoCecilMethod == null) _monoCecilMethod = typeInfo.ImportReference(_method).Resolve();
                return method.ToMethodDefinition().ToString() == _monoCecilMethod.ToString();
            }
            return false;
        }

        public ParameterInfo[] GetParameters() => _method.GetParameters();

        public override bool Equals(object obj)
            => obj is SourceMethod method && _method.Equals(method._method);

        public override int GetHashCode() => _method.GetHashCode();

        public override string ToString() => _method.ToString();
    }
}
