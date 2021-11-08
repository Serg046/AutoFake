using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup
{
    internal class SourceMethod : SourceMember, ISourceMember
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
            ReturnType = sourceMethod.DeclaringType ?? throw new InvalidOperationException("Declaring type should be set");
            HasStackInstance = false;
        }

        public string Name { get; }

        public Type ReturnType { get; }

        public bool HasStackInstance { get; }

        public MemberInfo OriginalMember => _method;

        public MethodDefinition GetMethod(IAssemblyWriter assemblyWriter)
	        => _monoCecilMethodDef ??= assemblyWriter.ImportToSourceAsm(_method).Resolve();

        public IList<GenericArgument> GetGenericArguments(IAssemblyWriter assemblyWriter)
        {
	        return _genericArguments ??= GetGenericArgumentsImpl(assemblyWriter).ToList();
        }

        private IEnumerable<GenericArgument> GetGenericArgumentsImpl(IAssemblyWriter assemblyWriter)
        {
	        var declaringType = GetMethod(assemblyWriter).DeclaringType.ToString();
	        if (_method.DeclaringType?.IsGenericType == true)
	        {
		        var types = _method.DeclaringType.GetGenericArguments();
		        var names = _method.DeclaringType.GetGenericTypeDefinition().GetGenericArguments();
		        foreach (var genericArgument in GetGenericArguments(assemblyWriter, types, names, declaringType))
		        {
			        yield return genericArgument;
		        }
	        }

	        if (_method.IsGenericMethod && _method is MethodInfo method)
	        {
		        var types = method.GetGenericArguments();
		        var names = method.GetGenericMethodDefinition().GetGenericArguments();
		        foreach (var genericArgument in GetGenericArguments(assemblyWriter, types, names, declaringType))
		        {
			        yield return genericArgument;
		        }
	        }
        }

        public bool IsSourceInstruction(IAssemblyWriter assemblyWriter, Instruction instruction, IEnumerable<GenericArgument> genericArguments)
        {
            if (instruction.OpCode.OperandType == OperandType.InlineMethod &&
                instruction.Operand is MethodReference method &&
                method.Name == _method.Name)
            {
	            var methodDef = method.ToMethodDefinition();
	            return methodDef.ToString() == GetMethod(assemblyWriter).ToString() && CompareGenericArguments(methodDef, genericArguments, assemblyWriter);
            }
            return false;
        }

        private bool CompareGenericArguments(MethodDefinition visitedMethod, IEnumerable<GenericArgument> genericArguments, IAssemblyWriter assemblyWriter)
        {
	        if (visitedMethod.HasGenericParameters || visitedMethod.DeclaringType.HasGenericParameters)
	        {
		        var sourceArguments = GetGenericArguments(assemblyWriter);
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

        public override bool Equals(object? obj)
            => obj is SourceMethod method && _method.Equals(method._method);

        public override int GetHashCode() => _method.GetHashCode();

        public override string? ToString() => _method.ToString();
	}
}
