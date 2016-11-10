using GuardExtensions;

namespace AutoFake
{
    internal class MockerFactory
    {
        public virtual IMocker CreateMocker(TypeInfo typeInfo, MockedMemberInfo mockedMemberInfo)
        {
            Guard.AreNotNull(typeInfo, mockedMemberInfo);
            return new Mocker(typeInfo, mockedMemberInfo);
        }

        public virtual IMethodInjector CreateMethodInjector(IMethodMocker mocker)
        {
            Guard.IsNotNull(mocker);
            return new MethodInjector(mocker);
        }
    }
}
