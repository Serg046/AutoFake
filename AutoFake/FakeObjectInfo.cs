using System;

namespace AutoFake
{
    internal class FakeObjectInfo
    {
	    public delegate FakeObjectInfo Create(Type sourceType, Type? fieldsType, object? instance);
        public FakeObjectInfo(Type sourceType, Type? fieldsType, object? instance)
	    {
            SourceType = sourceType;
            FieldsType = fieldsType;
            Instance = instance;
        }

        public Type SourceType { get; }
        public Type? FieldsType { get; }
        public object? Instance { get; }
    }
}
