using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class SourceMethod : ISourceMember
    {
        private readonly MethodBase _method;
        private MethodDefinition? _monoCecilMethodDef;
        private IList<GenericArgument>? _genericArguments;

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

        public MethodDefinition GetMethod(ITypeInfo typeInfo)
	        => _monoCecilMethodDef ??= typeInfo.ImportReference(_method).Resolve();

        public IList<GenericArgument> GetGenericArguments(ITypeInfo typeInfo)
        {
	        return _genericArguments ??= GetGenericArgumentsImpl(typeInfo).ToList();
        }

        private IEnumerable<GenericArgument> GetGenericArgumentsImpl(ITypeInfo typeInfo)
        {
	        var declaringType = GetMethod(typeInfo).DeclaringType.ToString();
	        if (_method.DeclaringType.IsGenericType)
	        {
		        var types = _method.DeclaringType.GetGenericArguments();
		        var names = _method.DeclaringType.GetGenericTypeDefinition().GetGenericArguments();
		        for (int i = 0; i < types.Length; i++)
		        {
			        var typeRef = typeInfo.ImportReference(types[i]);
			        yield return new GenericArgument(names[i].ToString(), typeRef.ToString(), declaringType);
		        }
	        }

	        if (_method.IsGenericMethod && _method is MethodInfo method)
	        {
		        var types = method.GetGenericArguments();
		        var names = method.GetGenericMethodDefinition().GetGenericArguments();
		        for (int i = 0; i < types.Length; i++)
		        {
			        var typeRef = typeInfo.ImportReference(types[i]);
			        yield return new GenericArgument(names[i].ToString(), typeRef.ToString(), declaringType);
		        }
	        }
        }

        public bool IsSourceInstruction(ITypeInfo typeInfo, Instruction instruction, IEnumerable<GenericArgument> genericArguments)
        {
            if (instruction.OpCode.OperandType == OperandType.InlineMethod &&
                instruction.Operand is MethodReference method &&
                method.Name == _method.Name)
            {
	            var methodDef = method.ToMethodDefinition();
	            return methodDef.ToString() == GetMethod(typeInfo).ToString() &&
	                   (!method.IsGenericInstance || CompareGenericArguments(methodDef, genericArguments, typeInfo));
            }
            return false;
        }

        private bool CompareGenericArguments(MethodDefinition visitedMethod, IEnumerable<GenericArgument> genericArguments, ITypeInfo typeInfo)
        {
	        if (visitedMethod.HasGenericParameters || visitedMethod.DeclaringType.HasGenericParameters)
	        {
		        var arguments = GetGenericArguments(typeInfo);
		        foreach (var genericParameter in visitedMethod.GenericParameters.Concat(visitedMethod.DeclaringType.GenericParameters))
		        {
			        var source = arguments.SingleOrDefault(a => a.Name == genericParameter.Name);
			        var visited = genericArguments.FindGenericTypeOrDefault(genericParameter.Name);
			        if (source == null || visited == null || source.Type != visited.Type)
			        {
				        return false;
			        }
		        }
	        }

	        return true;
        }

        public ParameterInfo[] GetParameters() => _method.GetParameters();

        public override bool Equals(object obj)
            => obj is SourceMethod method && _method.Equals(method._method);

        public override int GetHashCode() => _method.GetHashCode();

        public override string ToString() => _method.ToString();
	}
}
