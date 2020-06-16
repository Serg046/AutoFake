using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class MockConfigurationTests
    {
        private readonly IProcessorFactory _procFactory = new ProcessorFactory(
            new TypeInfo(typeof(TestClass), new FakeDependency[0]));

        [Fact]
        public void Replace_CfgInvalidInput_Throws()
        {
            var cfg = new MockConfiguration(new List<IMock>(), _procFactory);
            Assert.Throws<ArgumentNullException>(() => cfg.Replace((Expression<Func<TestClass, object>>)null));
            Assert.Throws<ArgumentNullException>(() => cfg.Replace((Expression<Func<object>>)null));
        }

        [Fact]
        public void Replace_GenericCfgInvalidInput_Throws()
        {
            var genericCfg = new MockConfiguration<TestClass>(new List<IMock>(), _procFactory);
            MockConfiguration cfg = genericCfg;
            Assert.Throws<ArgumentNullException>(() => cfg.Replace((Expression<Func<TestClass, object>>)null));
            Assert.Throws<ArgumentNullException>(() => cfg.Replace((Expression<Func<object>>)null));
            Assert.Throws<ArgumentNullException>(() => genericCfg.Replace((Expression<Func<TestClass, object>>)null));
        }

        [Fact]
        public void Remove_FakeInvalidInput_Throws()
        {
            var cfg = new MockConfiguration(new List<IMock>(), _procFactory);
            Assert.Throws<ArgumentNullException>(() => cfg.Remove((Expression<Action<TestClass>>)null));
            Assert.Throws<ArgumentNullException>(() => cfg.Remove((Expression<Action>)null));
        }

        [Fact]
        public void Remove_GenericFakeInvalidInput_Throws()
        {
            var genericCfg = new MockConfiguration<TestClass>(new List<IMock>(), _procFactory);
            MockConfiguration cfg = genericCfg;
            Assert.Throws<ArgumentNullException>(() => cfg.Remove((Expression<Action<TestClass>>)null));
            Assert.Throws<ArgumentNullException>(() => cfg.Remove((Expression<Action>)null));
            Assert.Throws<ArgumentNullException>(() => genericCfg.Remove((Expression<Action<TestClass>>)null));
        }

        [Theory]
        [MemberData(nameof(GetFuncExpressions))]
        public void Replace_Fake_MockAdded(dynamic callback)
        {
            var cfg = new MockConfiguration(new List<IMock>(), _procFactory);

            cfg.Replace(callback);

            Assert.NotEmpty(cfg.Mocks);
        }

        [Theory]
        [MemberData(nameof(GetFuncExpressions))]
        internal void Replace_GenericFake_MockAdded(dynamic callback)
        {
            var cfg = new MockConfiguration<TestClass>(new List<IMock>(), _procFactory);

            cfg.Replace(callback);

            Assert.NotEmpty(cfg.Mocks);
        }

        [Theory]
        [MemberData(nameof(GetActionExpressions))]
        public void Remove_Fake_MockAdded(dynamic callback)
        {
            var cfg = new MockConfiguration(new List<IMock>(), _procFactory);

            cfg.Remove(callback);

            Assert.NotEmpty(cfg.Mocks);
        }

        [Theory]
        [MemberData(nameof(GetActionExpressions))]
        internal void Remove_GenericFake_MockAdded(dynamic callback)
        {
            var cfg = new MockConfiguration<TestClass>(new List<IMock>(), _procFactory);

            cfg.Remove(callback);

            Assert.NotEmpty(cfg.Mocks);
        }

        [Fact]
        public void Verify_FakeInvalidInput_Throws()
        {
            var cfg = new MockConfiguration(new List<IMock>(), _procFactory);
            Assert.Throws<ArgumentNullException>(() => cfg.Verify((Expression<Func<TestClass, object>>)null));
            Assert.Throws<ArgumentNullException>(() => cfg.Verify((Expression<Action<TestClass>>)null));
            Assert.Throws<ArgumentNullException>(() => cfg.Verify((Expression<Func<object>>)null));
            Assert.Throws<ArgumentNullException>(() => cfg.Verify((Expression<Action>)null));
        }

        [Fact]
        public void Verify_GenericFakeInvalidInput_Throws()
        {
            var genericCfg = new MockConfiguration<TestClass>(new List<IMock>(), _procFactory);
            MockConfiguration cfg = genericCfg;
            Assert.Throws<ArgumentNullException>(() => cfg.Verify((Expression<Func<TestClass, object>>)null));
            Assert.Throws<ArgumentNullException>(() => cfg.Verify((Expression<Action<TestClass>>)null));
            Assert.Throws<ArgumentNullException>(() => cfg.Verify((Expression<Func<object>>)null));
            Assert.Throws<ArgumentNullException>(() => cfg.Verify((Expression<Action>)null));
            Assert.Throws<ArgumentNullException>(() => genericCfg.Verify((Expression<Func<TestClass, object>>)null));
            Assert.Throws<ArgumentNullException>(() => genericCfg.Verify((Expression<Action<TestClass>>)null));
        }

        [Theory]
        [MemberData(nameof(GetCallbackExpressions))]
        public void Verify_Fake_MockAdded(dynamic callback)
        {
            var cfg = new MockConfiguration(new List<IMock>(), _procFactory);

            cfg.Verify(callback);

            Assert.NotEmpty(cfg.Mocks);
        }

        [Theory]
        [MemberData(nameof(GetCallbackExpressions))]
        internal void Verify_GenericFake_MockAdded(dynamic callback)
        {
            var cfg = new MockConfiguration<TestClass>(new List<IMock>(), _procFactory);

            cfg.Verify(callback);

            Assert.NotEmpty(cfg.Mocks);
        }

        [Fact]
        public void Append_GenericFake_MockAdded()
        {
            var cfg = new MockConfiguration<TestClass>(new List<IMock>(), _procFactory);
            Action callback = () => { };

            cfg.Append(callback);

            var mock = Assert.IsType<InsertMock>(cfg.Mocks.Single());
            Assert.Equal(callback, mock.Closure);
        }

        [Fact]
        public void Append_Fake_MockAdded()
        {
            var cfg = new MockConfiguration(new List<IMock>(), _procFactory);
            Action callback = () => { };

            cfg.Append(callback);

            var mock = Assert.IsType<InsertMock>(cfg.Mocks.Single());
            Assert.Equal(callback, mock.Closure);
        }

        [Fact]
        public void Prepend_GenericFake_MockAdded()
        {
            var cfg = new MockConfiguration<TestClass>(new List<IMock>(), _procFactory);
            Action callback = () => { };

            cfg.Prepend(callback);

            var mock = Assert.IsType<InsertMock>(cfg.Mocks.Single());
            Assert.Equal(callback, mock.Closure);
        }

        [Fact]
        public void Prepend_Fake_MockAdded()
        {
            var cfg = new MockConfiguration(new List<IMock>(), _procFactory);
            Action callback = () => { };

            cfg.Prepend(callback);

            var mock = Assert.IsType<InsertMock>(cfg.Mocks.Single());
            Assert.Equal(callback, mock.Closure);
        }

        public static IEnumerable<object[]> GetCallbackExpressions()
            => GetFuncExpressions().Concat(GetActionExpressions());

        public static IEnumerable<object[]> GetFuncExpressions()
        {
            Expression<Func<TestClass, string>> instanceFunc = tst => tst.StringInstanceMethod();
            yield return new object[] { instanceFunc };
            Expression<Func<string>> staticFunc = () => TestClass.StaticStringInstanceMethod();
            yield return new object[] { staticFunc };
        }

        public static IEnumerable<object[]> GetActionExpressions()
        {
            Expression<Action<TestClass>> instanceAction = tst => tst.VoidInstanceMethod();
            yield return new object[] { instanceAction };
            Expression<Action> staticAction = () => TestClass.StaticVoidInstanceMethod();
            yield return new object[] { staticAction };
        }

        public static IEnumerable<object[]> GetActions()
        {
            Action<TestClass> instanceAction = tst => tst.VoidInstanceMethod();
            yield return new object[] { instanceAction };
            Action staticAction = () => TestClass.StaticVoidInstanceMethod();
            yield return new object[] { staticAction };
        }

        internal class TestClass
        {
            public const int CONST_FIELD = 5;

            public void SomeMethod() { }

            internal void FailingMethod() => throw new NotImplementedException();
            internal void VoidInstanceMethod() { }
            internal string StringInstanceMethod() => string.Empty;
            internal static void StaticVoidInstanceMethod() { }
            internal static string StaticStringInstanceMethod() => string.Empty;

            internal async Task FailingMethodAsync()
            {
                await Task.Delay(1);
                throw new NotImplementedException();
            }
        }
    }
}
