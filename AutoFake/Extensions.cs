using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil;
using System.Linq;
using Mono.Cecil.Cil;

namespace AutoFake
{
    internal static class Extensions
    {
        public static bool EquivalentTo(this MethodReference methodReference, MethodReference method)
            => methodReference.Name == method.Name &&
               methodReference.Parameters.Select(p => p.ParameterType.FullName)
         .SequenceEqual(method.Parameters.Select(p => p.ParameterType.FullName)) &&
               methodReference.ReturnType.FullName == method.ReturnType.FullName;
        
        public static bool IsAsync(this MethodDefinition method, out MethodDefinition asyncMethod)
        {
            var asyncAttribute = method.CustomAttributes
                .SingleOrDefault(a => a.AttributeType.Name == "AsyncStateMachineAttribute");
            if (asyncAttribute != null)
            {
                var typeRef = asyncAttribute.ConstructorArguments[0].Value as TypeReference;
                asyncMethod = typeRef.ToTypeDefinition().Methods.Single(m => m.Name == "MoveNext");
                return true;
            }
            asyncMethod = null;
            return false;
        }

        public static TypeDefinition ToTypeDefinition(this TypeReference type)
            => type as TypeDefinition ?? type.Resolve();

        public static FieldDefinition ToFieldDefinition(this FieldReference field)
            => field as FieldDefinition ?? field.Resolve();

        public static MethodDefinition ToMethodDefinition(this MethodReference method)
            => method as MethodDefinition ?? method.Resolve();

        public static IEqualityComparer ToNonGeneric<T>(this IEqualityComparer<T> comparer)
            => new EqualityComparer((x, y) => comparer.Equals((T)x, (T)y), x => comparer.GetHashCode((T)x));

        public static MethodReference ReplaceDeclaringType(this MethodReference methodDef, TypeReference declaringTypeRef)
        {
	        var methodRef = new MethodReference(methodDef.Name, methodDef.ReturnType, declaringTypeRef)
	        {
		        CallingConvention = methodDef.CallingConvention,
		        HasThis = methodDef.HasThis,
		        ExplicitThis = methodDef.ExplicitThis
	        };

	        foreach (var paramDef in methodDef.Parameters)
	        {
		        methodRef.Parameters.Add(new ParameterDefinition(paramDef.Name, paramDef.Attributes, paramDef.ParameterType));
	        }

	        foreach (var genParamDef in methodDef.GenericParameters)
	        {
		        methodRef.GenericParameters.Add(new GenericParameter(genParamDef.Name, methodRef));
	        }

	        return methodRef;
        }

        public static Instruction Copy(this Instruction instruction)
        {
	        if (instruction is null) throw new ArgumentNullException(nameof(instruction));
            if (instruction.Operand is null) return Instruction.Create(instruction.OpCode);

            return instruction.Operand switch
            {
	            TypeReference operand => Instruction.Create(instruction.OpCode, operand),
	            CallSite operand => Instruction.Create(instruction.OpCode, operand),
	            MethodReference operand => Instruction.Create(instruction.OpCode, operand),
	            FieldReference operand => Instruction.Create(instruction.OpCode, operand),
	            string operand => Instruction.Create(instruction.OpCode, operand),
	            sbyte operand => Instruction.Create(instruction.OpCode, operand),
	            byte operand => Instruction.Create(instruction.OpCode, operand),
	            int operand => Instruction.Create(instruction.OpCode, operand),
	            long operand => Instruction.Create(instruction.OpCode, operand),
	            float operand => Instruction.Create(instruction.OpCode, operand),
	            double operand => Instruction.Create(instruction.OpCode, operand),
	            Instruction operand => Instruction.Create(instruction.OpCode, operand),
	            Instruction[] operand => Instruction.Create(instruction.OpCode, operand),
	            VariableDefinition operand => Instruction.Create(instruction.OpCode, operand),
	            ParameterDefinition operand => Instruction.Create(instruction.OpCode, operand),
	            _ => throw new NotSupportedException("The operand is not supported")
            };
        }

        public static Instruction ShiftDown(this IEmitter emitter, Instruction instruction)
        {
	        var copy = instruction.Copy();
	        instruction.OpCode = OpCodes.Nop;
	        instruction.Operand = null;
	        emitter.InsertAfter(instruction, copy);
	        return copy;
        }

        public static MethodContract ToMethodContract(this MethodDefinition method)
	        => new(method.DeclaringType.ToString(), method.ReturnType.ToString(), method.Name,
		        method.Parameters.Select(p => p.ParameterType.ToString()).ToArray());

        private class EqualityComparer : IEqualityComparer
        {
            private readonly Func<object, object, bool> _comparer;
            private readonly Func<object, int> _hasher;

            public EqualityComparer(Func<object, object, bool> comparer, Func<object, int> hasher)
            {
                _comparer = comparer;
                _hasher = hasher;
            }

            public bool Equals(object x, object y) => _comparer(x, y);
            public int GetHashCode(object obj) => _hasher(obj);
        }
    }
}
