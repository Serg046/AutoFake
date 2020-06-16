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
        
        public static IEmitter GetEmitter(this MethodBody method) => new Emitter(method);

        public static bool IsAsync(this MethodDefinition method, out MethodDefinition asyncMethod)
        {
            var asyncAttribute = method.CustomAttributes
                .SingleOrDefault(a => a.AttributeType.Name == "AsyncStateMachineAttribute");
            if (asyncAttribute != null)
            {
                var generatedAsyncType = (TypeReference)asyncAttribute.ConstructorArguments[0].Value;
                asyncMethod = generatedAsyncType.Resolve().Methods.Single(m => m.Name == "MoveNext");
                return true;
            }
            asyncMethod = null;
            return false;
        }
    }
}
