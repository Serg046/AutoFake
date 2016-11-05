using System;

namespace AutoFake
{
    public class FakeDependency
    {
        internal FakeDependency(Type type, object instane)
        {
            Type = type;
            Instance = instane;
        }

        public Type Type { get; }
        public object Instance { get; }
    }
}
