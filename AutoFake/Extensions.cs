using System.Reflection;
using Mono.Cecil;
using System.Linq;

namespace AutoFake
{
    internal static class Extensions
    {
        public static bool EquivalentTo(this MethodReference methodReference, MethodBase method)
            => methodReference.Name == method.Name &&
               methodReference.Parameters.Select(p => p.ParameterType.FullName)
                   .SequenceEqual(method.GetParameters().Select(p => p.ParameterType.FullName));
    }
}
