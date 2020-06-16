using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class PrependMockInstallerTests
    {
        [Theory, MemberAutoMoqData(nameof(GetActions))]
        internal void Before_Installer_MockReplaced(
            dynamic callback,
            Action descriptor,
            IProcessorFactory factory)
        {
            var mocks = new List<IMock>();
            mocks.Add(null);
            var installer = new PrependMockConfiguration(factory, (m, i) => mocks[i] = m, 0, descriptor);

            installer.Before(callback);

            var mock = Assert.IsType<SourceMemberInsertMock>(mocks.Single());
            Assert.Equal(descriptor, mock.Closure);
        }

        [Theory, MemberAutoMoqData(nameof(GetActions))]
        internal void Before_GenericInstaller_MockReplaced(
            dynamic callback,
            Action descriptor,
            IProcessorFactory factory)
        {
            var mocks = new List<IMock>();
            mocks.Add(null);
            var installer = new PrependMockConfiguration<TestClass>(factory, (m, i) => mocks[i] = m, 0, descriptor);

            installer.Before(callback);

            var mock = Assert.IsType<SourceMemberInsertMock>(mocks.Single());
            Assert.Equal(descriptor, mock.Closure);
        }

        public static IEnumerable<object[]> GetActions()
        {
            Expression<Action<TestClass>> instanceAction = tst => tst.VoidInstanceMethod();
            yield return new object[] { instanceAction };
            Expression<Action> staticAction = () => TestClass.StaticVoidInstanceMethod();
            yield return new object[] { staticAction };
        }

        private class TestClass
        {
            internal void VoidInstanceMethod() { }
            internal static void StaticVoidInstanceMethod() { }
        }
    }
}
