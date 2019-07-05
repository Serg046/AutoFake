namespace AutoFake
{
    internal class MockerFactory
    {
        public virtual IMocker CreateMocker(ITypeInfo typeInfo, MockedMemberInfo mockedMemberInfo) => new Mocker(typeInfo, mockedMemberInfo);
    }
}
