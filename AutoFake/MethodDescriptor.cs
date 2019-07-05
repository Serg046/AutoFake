namespace AutoFake
{
    internal class MethodDescriptor
    {
        public MethodDescriptor(string declaringType, string name)
        {
            DeclaringType = declaringType;
            Name = name;
        }

        public string DeclaringType { get; }
        public string Name { get; }
    }
}
