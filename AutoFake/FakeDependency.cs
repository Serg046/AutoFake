using System;

namespace AutoFake
{
    public class FakeDependency
    {
        internal FakeDependency(Type? type, object? instance)
        {
            Type = type;
            Instance = instance;
        }

        public Type? Type { get; }
        public object? Instance { get; }
    }
}
