using System;

namespace AutoFake
{
    public class FakeDependency
    {
        private FakeDependency(Type type, object instane)
        {
            Type = type;
            Instance = instane;
        }

        public static FakeDependency Null<T>() where T : class
            => new FakeDependency(typeof(T), null);

        internal static FakeDependency Create(Type type, object instane)
            => new FakeDependency(type, instane);

        public Type Type { get; }
        public object Instance { get; }
    }
}
