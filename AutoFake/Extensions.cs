using System;
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

        public static MethodDescriptor ToMethodDescriptor(this Delegate action)
            => new MethodDescriptor(action.Method.DeclaringType?.FullName, action.Method.Name);

        public static IEmitter GetEmitter(this Mono.Cecil.Cil.MethodBody method) => new Emitter(method);

        public static bool IsAsync(this MethodDefinition method, out MethodDefinition asyncMethod)
        {
            //for .net 4, it is available in .net 4.5
            dynamic asyncAttribute = method.CustomAttributes
                .SingleOrDefault(a => a.AttributeType.Name == "AsyncStateMachineAttribute");
            if (asyncAttribute != null)
            {
                TypeReference generatedAsyncType = asyncAttribute.ConstructorArguments[0].Value;
                asyncMethod = generatedAsyncType.Resolve().Methods.Single(m => m.Name == "MoveNext");
                return true;
            }
            asyncMethod = null;
            return false;
        }

        public static void ReplaceType(this Instruction instruction, TypeReference typeRef)
        {
            if (instruction.Operand is FieldReference field && field.FieldType.FullName == typeRef.FullName)
            {
                field.FieldType = typeRef;
            }
            else if (instruction.Operand is MethodReference method)
            {
                if (method.ReturnType.FullName == typeRef.FullName)
                {
                    method.ReturnType = typeRef;
                }
                for (var i = 0; i < method.Parameters.Count; i++)
                {
                    if (method.Parameters[i].ParameterType.FullName == typeRef.FullName)
                    {
                        method.Parameters[i].ParameterType = typeRef;
                    }
                }
            }
        }
    }
}
