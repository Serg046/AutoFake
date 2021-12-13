using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class AppendMockInstallerTests
    {
        [Theory, MemberAutoMoqData(nameof(GetCallbacks))]
        internal void After_Installer_MockReplaced(
            dynamic callback,
            Action descriptor,
            IProcessorFactory factory)
        {
            var mocks = new List<IMock>();
            mocks.Add(null);
            var installer = new AppendMockConfiguration(factory, m => mocks[0] = m, descriptor);

            installer.After(callback);

            var mock = Assert.IsType<SourceMemberInsertMock>(mocks.Single());
            Assert.Equal(descriptor, mock.Closure);
        }

        [Theory, MemberAutoMoqData(nameof(GetCallbacks))]
        internal void After_GenericInstaller_MockReplaced(
            dynamic callback,
            Action descriptor,
            IProcessorFactory factory)
        {
            var mocks = new List<IMock>();
            mocks.Add(null);
            var installer = new AppendMockConfiguration<TestClass>(factory, m => mocks[0] = m, descriptor);

            installer.After(callback);

            var mock = Assert.IsType<SourceMemberInsertMock>(mocks.Single());
            Assert.Equal(descriptor, mock.Closure);
        }

        public static IEnumerable<object[]> GetCallbacks()
            => GetFuncs().Concat(GetActions());

        public static IEnumerable<object[]> GetActions()
        {
            Expression<Action<TestClass>> instanceAction = tst => tst.VoidInstanceMethod();
            yield return new object[] { instanceAction };
            Expression<Action> staticAction = () => TestClass.VoidStaticMethod();
            yield return new object[] { staticAction };
        }
        public static IEnumerable<object[]> GetFuncs()
        {
            Expression<Func<TestClass, int>> instanceFunc = tst => tst.IntInstanceMethod();
            yield return new object[] { instanceFunc };
            Expression<Func<int>> staticFunc = () => TestClass.IntStaticMethod();
            yield return new object[] { staticFunc };
        }


        private class TestClass
        {
            internal void VoidInstanceMethod() { }
            internal int IntInstanceMethod() => 5;
            internal static void VoidStaticMethod() { }
            internal static int IntStaticMethod() => 5;
        }
    }
}
