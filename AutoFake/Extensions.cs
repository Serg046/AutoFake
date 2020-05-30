using System;
using System.Reflection;
using Mono.Cecil;
using System.Linq;

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
    }
}
