namespace AutoFake
{
    internal class MockerFactory
    {
        public virtual IMocker CreateMocker(TypeInfo typeInfo, MockedMemberInfo mockedMemberInfo) => new Mocker(typeInfo, mockedMemberInfo);

        public virtual IMethodInjector CreateMethodInjector(IMethodMocker mocker) => new MethodInjector(mocker);
    }
}
