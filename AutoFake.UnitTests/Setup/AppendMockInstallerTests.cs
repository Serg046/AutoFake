using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoFake.Setup;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class AppendMockInstallerTests
    {
        [Theory, MemberData(nameof(GetActions))]
        public void After_Installer_MockReplaced(dynamic callback)
        {
            var mocks = new List<IMock>();
            mocks.Add(null);
            var descriptor = new MethodDescriptor("testType", "testMethodName");
            var installer = new AppendMockInstaller(mocks, 0, descriptor);

            installer.After(callback);

            var mock = Assert.IsType<SourceMemberInsertMock>(mocks.Single());
            Assert.Equal(descriptor, mock.Action);
        }

        [Theory, MemberData(nameof(GetActions))]
        public void After_GenericInstaller_MockReplaced(dynamic callback)
        {
            var mocks = new List<IMock>();
            mocks.Add(null);
            var descriptor = new MethodDescriptor("testType", "testMethodName");
            var installer = new AppendMockInstaller<TestClass>(mocks, 0, descriptor);

            installer.After(callback);

            var mock = Assert.IsType<SourceMemberInsertMock>(mocks.Single());
            Assert.Equal(descriptor, mock.Action);
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
