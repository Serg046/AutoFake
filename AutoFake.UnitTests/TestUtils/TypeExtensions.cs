using Mono.Cecil;

namespace AutoFake.UnitTests.TestUtils
{
    public static class TypeExtensions
    {
        public static TypeReference ValueTypeRef() => new TypeReference("System", "ValueType", null, null, true);

        public static TypeDefinition ValueTypeDef() => new TypeDefinition("TestNs", "TestType", TypeAttributes.Class, ValueTypeRef());
    }
}
