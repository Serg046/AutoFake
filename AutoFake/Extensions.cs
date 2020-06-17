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

        public static IEqualityComparer ToNonGeneric<T>(this IEqualityComparer<T> comparer)
            => new EqualityComparer((x, y) => comparer.Equals((T)x, (T)y), x => comparer.GetHashCode((T)x));

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
