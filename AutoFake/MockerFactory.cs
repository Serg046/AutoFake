using AutoFake.Setup;
using GuardExtensions;

namespace AutoFake
{
    internal class MockerFactory
    {
        public virtual IMocker CreateMocker(TypeInfo typeInfo, FakeSetupPack setup)
        {
            Guard.AreNotNull(typeInfo, setup);
            return new Mocker(typeInfo, setup);
        }

        public virtual IMethodInjector CreateMethodInjector(IMethodMocker mocker)
        {
            Guard.IsNotNull(mocker);
            return new MethodInjector(mocker);
        }
    }
}
