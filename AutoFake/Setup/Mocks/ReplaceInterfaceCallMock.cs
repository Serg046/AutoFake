using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoFake.Setup.Mocks
{
    internal class ReplaceInterfaceCallMock : IMock
    {
	    private readonly ITypeInfo _typeInfo;
	    private readonly Type _type;
        private readonly string _typeContract;

        private ReplaceInterfaceCallMock(ITypeInfo typeInfo, Type type)
        {
	        _typeInfo = typeInfo;
	        _type = type;
            _typeContract = typeInfo.ImportReference(type).ToString();
        }

        public static IEnumerable<ReplaceInterfaceCallMock> Create(ITypeInfo typeInfo, Type type)
        {
	        if (type.IsInterface) yield return new ReplaceInterfaceCallMock(typeInfo, type);
	        foreach (var @interface in type.GetInterfaces())
	        {
		        yield return new ReplaceInterfaceCallMock(typeInfo, @interface);
	        }
        }

        public bool IsSourceInstruction(MethodDefinition method, Instruction instruction)
	        => instruction != null && instruction.OpCode.OperandType == OperandType.InlineMethod
	                               && instruction.Operand is MethodReference m
	                               && m.DeclaringType.ToString() == _typeContract;

        [ExcludeFromCodeCoverage]
        public void BeforeInjection(MethodDefinition method)
        {
        }

        public void Inject(IEmitter emitter, Instruction instruction)
        {
	        var method = GetMethod(instruction);
	        var newInstruction = Instruction.Create(instruction.OpCode, _typeInfo.ImportReference(method));
	        emitter.Replace(instruction, newInstruction);
        }

        private MethodInfo GetMethod(Instruction instruction)
        {
	        var methodRef = ((MethodReference)instruction.Operand);
	        try
	        {
		        return _type.GetMethod(methodRef.Name,
			        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
	        }
	        catch (AmbiguousMatchException)
	        {
		        var types = methodRef.Parameters.Select(p => TypeInfo.GetClrName(p.ParameterType.ToString()));
                var methods = _type.GetMethods(
	                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
	                .Where(m => m.Name == methodRef.Name);
                return methods.First(m => m.GetParameters()
	                .Select(p => p.ParameterType.ToString())
	                .SequenceEqual(types));
	        }
        }

        [ExcludeFromCodeCoverage]
        public void AfterInjection(IEmitter emitter)
        {
        }

        public IList<object> Initialize(Type type) => new List<object>();

        public override int GetHashCode() => _type.GetHashCode();

        public override bool Equals(object obj) => obj is ReplaceInterfaceCallMock mock && mock._type == _type;
    }
}
