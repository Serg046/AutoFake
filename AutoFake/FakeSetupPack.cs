using System.Collections.Generic;
using System.Reflection;

namespace AutoFake
{
    internal class FakeSetupPack
    {
        public FakeSetupPack(MethodInfo method, object returnObject, IEnumerable<MethodInfo> reachableWithCollection)
        {
            Method = method;
            ReturnObject = returnObject;
            ReachableWithCollection = reachableWithCollection;
        }

        public MethodInfo Method { get; }
        public object ReturnObject { get; }
        public IEnumerable<MethodInfo> ReachableWithCollection { get; }
    }
}
