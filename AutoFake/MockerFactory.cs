namespace AutoFake
{
    internal class MockerFactory
    {
        public IMocker CreateMocker(ITypeInfo typeInfo, MockedMemberInfo mockedMemberInfo) => new Mocker(typeInfo, mockedMemberInfo);
    }
}
