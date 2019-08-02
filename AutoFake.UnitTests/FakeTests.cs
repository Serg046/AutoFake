using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AutoFake.Exceptions;
using AutoFake.Setup;
using Mono.Cecil;
using Moq;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeTests
    {
        [Fact]
        public void Ctor_InvalidInput_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Fake<TestClass>(null));
            Assert.Throws<ArgumentNullException>(() => new Fake(null));
            Assert.Throws<ArgumentNullException>(() => new Fake(typeof(TestClass), null));
        }

        [Fact]
        public void Replace_FakeInvalidInput_Throws()
        {
            var fake = new Fake(typeof(TestClass));
            Assert.Throws<ArgumentNullException>(() => fake.Replace((Expression<Func<TestClass, object>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Replace((Expression<Func<object>>)null));
        }

        [Fact]
        public void Replace_GenericFakeInvalidInput_Throws()
        {
            var genericFake = new Fake<TestClass>();
            Fake fake = genericFake;
            Assert.Throws<ArgumentNullException>(() => fake.Replace((Expression<Func<TestClass, object>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Replace((Expression<Func<object>>)null));
            Assert.Throws<ArgumentNullException>(() => genericFake.Replace((Expression<Func<TestClass, object>>)null));
        }

        [Fact]
        public void Remove_FakeInvalidInput_Throws()
        {
            var fake = new Fake(typeof(TestClass));
            Assert.Throws<ArgumentNullException>(() => fake.Remove((Expression<Action<TestClass>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Remove((Expression<Action>)null));
        }

        [Fact]
        public void Remove_GenericFakeInvalidInput_Throws()
        {
            var genericFake = new Fake<TestClass>();
            Fake fake = genericFake;
            Assert.Throws<ArgumentNullException>(() => fake.Remove((Expression<Action<TestClass>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Remove((Expression<Action>)null));
            Assert.Throws<ArgumentNullException>(() => genericFake.Remove((Expression<Action<TestClass>>)null));
        }

        [Theory]
        [MemberData(nameof(GetFuncs))]
        public void Replace_Fake_MockAdded(dynamic callback)
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(callback);

            Assert.NotEmpty(fake.Mocks);
        }

        [Theory]
        [MemberData(nameof(GetFuncs))]
        internal void Replace_GenericFake_MockAdded(dynamic callback)
        {
            var fake = new Fake<TestClass>();

            fake.Replace(callback);

            Assert.NotEmpty(fake.Mocks);
        }

        [Theory]
        [MemberData(nameof(GetActions))]
        public void Remove_Fake_MockAdded(dynamic callback)
        {
            var fake = new Fake(typeof(TestClass));

            fake.Remove(callback);

            Assert.NotEmpty(fake.Mocks);
        }

        [Theory]
        [MemberData(nameof(GetActions))]
        internal void Remove_GenericFake_MockAdded(dynamic callback)
        {
            var fake = new Fake<TestClass>();

            fake.Remove(callback);

            Assert.NotEmpty(fake.Mocks);
        }

        [Fact]
        public void Verify_FakeInvalidInput_Throws()
        {
            var fake = new Fake(typeof(TestClass));
            Assert.Throws<ArgumentNullException>(() => fake.Verify((Expression<Func<TestClass, object>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Verify((Expression<Action<TestClass>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Verify((Expression<Func<object>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Verify((Expression<Action>)null));
        }

        [Fact]
        public void Verify_GenericFakeInvalidInput_Throws()
        {
            var genericFake = new Fake<TestClass>();
            Fake fake = genericFake;
            Assert.Throws<ArgumentNullException>(() => fake.Verify((Expression<Func<TestClass, object>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Verify((Expression<Action<TestClass>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Verify((Expression<Func<object>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Verify((Expression<Action>)null));
            Assert.Throws<ArgumentNullException>(() => genericFake.Verify((Expression<Func<TestClass, object>>)null));
            Assert.Throws<ArgumentNullException>(() => genericFake.Verify((Expression<Action<TestClass>>)null));
        }

        [Theory]
        [MemberData(nameof(GetCallbacks))]
        public void Verify_Fake_MockAdded(dynamic callback)
        {
            var fake = new Fake(typeof(TestClass));

            fake.Verify(callback);

            Assert.NotEmpty(fake.Mocks);
        }

        [Theory]
        [MemberData(nameof(GetCallbacks))]
        internal void Verify_GenericFake_MockAdded(dynamic callback)
        {
            var fake = new Fake<TestClass>();

            fake.Verify(callback);

            Assert.NotEmpty(fake.Mocks);
        }

        [Fact]
        public void Rewrite_FakeInvalidInput_Throws()
        {
            var fake = new Fake(typeof(TestClass));
            Assert.Throws<ArgumentNullException>(() => fake.Rewrite((Expression<Func<TestClass, object>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Rewrite((Expression<Action<TestClass>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Rewrite((Expression<Func<object>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Rewrite((Expression<Action>)null));
        }

        [Fact]
        public void Rewrite_GenericFakeInvalidInput_Throws()
        {
            var genericFake = new Fake<TestClass>();
            Fake fake = genericFake;
            Assert.Throws<ArgumentNullException>(() => fake.Rewrite((Expression<Func<TestClass, object>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Rewrite((Expression<Action<TestClass>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Rewrite((Expression<Func<object>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Rewrite((Expression<Action>)null));
            Assert.Throws<ArgumentNullException>(() => genericFake.Rewrite((Expression<Func<TestClass, object>>)null));
            Assert.Throws<ArgumentNullException>(() => genericFake.Rewrite((Expression<Action<TestClass>>)null));
        }

        [Theory]
        [MemberData(nameof(GetCallbacks))]
        public void Rewrite_Fake_Throws(dynamic callback)
        {
            var fake = new Fake(typeof(TestClass));
            var mock = new Mock<IMock>();
            fake.Mocks.Add(mock.Object);

            fake.Rewrite(callback);

            mock.Verify(m => m.BeforeInjection(It.IsAny<IMocker>()));
        }

        [Theory]
        [MemberData(nameof(GetCallbacks))]
        public void Rewrite_GenericFake_Throws(dynamic callback)
        {
            var fake = new Fake<TestClass>();
            var mock = new Mock<IMock>();
            fake.Mocks.Add(mock.Object);

            fake.Rewrite(callback);

            mock.Verify(m => m.BeforeInjection(It.IsAny<IMocker>()));
        }

        [Fact]
        public void Reset_ClearsSetups()
        {
            var fake = new Fake<FakeTests>();
            fake.Verify(f => f.Reset_ClearsSetups())
                .ExpectedCalls(1);
            Assert.NotEmpty(fake.Mocks);

            fake.Reset();
            Assert.Empty(fake.Mocks);
        }

        [Fact]
        public void SaveFakeAssembly_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Fake<FakeTests>().SaveFakeAssembly(null));
        }

        [Fact]
        public void SaveFakeAssembly_FileName_Saved()
        {
            var fileName = $"Unit-testing-{Guid.NewGuid()}.dll";
            var type = typeof(TestClass);
            var fake = new Fake(type);

            fake.SaveFakeAssembly(fileName);

            var assembly = AssemblyDefinition.ReadAssembly(fileName, new ReaderParameters(ReadingMode.Immediate));
            var savedType = assembly.MainModule.GetType(type.FullName, true).Resolve();
            Assert.Contains(savedType.Methods, m => m.Name == nameof(TestClass.VoidInstanceMethod));
            File.Delete(fileName);
        }

        [Fact]
        public void Execute_TargetInvocationException_InnerExceptionThrown()
        {
            var type = typeof(TestClass);
            var fake = new Fake(type);

            Assert.Throws<NotImplementedException>(() => fake.Execute(type.GetMethod(nameof(TestClass.FailingMethod),
                BindingFlags.Instance | BindingFlags.NonPublic), gen => new object[0]));
        }

        [Fact]
        public void Execute_NonTargetInvocationException_TheSameExceptionThrown()
        {
            var type = typeof(TestClass);
            var fake = new Fake(type);

            Assert.Throws<TargetParameterCountException>(() => fake.Execute(type.GetMethod(nameof(TestClass.FailingMethod),
                BindingFlags.Instance | BindingFlags.NonPublic), gen => new object[1]));
        }

        [Fact]
        public async Task Execute_GenericFake_CallbackExecuted()
        {
            const string testString = "testString";
            var fake = new Fake<TestClass>();
            fake.Replace(f => f.StringInstanceMethod()).Return(() => testString);
            fake.Rewrite(f => f.StringInstanceMethod());

            Assert.Throws<NotImplementedException>(() => fake.Execute(tst => tst.FailingMethod()));
            await Assert.ThrowsAsync<NotImplementedException>(() => fake.ExecuteAsync(tst => tst.FailingMethodAsync()));
            Assert.Throws<NotImplementedException>(() => fake.Execute((tst, prms) =>
            {
                Assert.Equal(testString, prms.Single());
                tst.FailingMethod();
            }));
            await Assert.ThrowsAsync<NotImplementedException>(() => fake.ExecuteAsync(async (tst, prms) =>
            {
                Assert.Equal(testString, prms.Single());
                await tst.FailingMethodAsync();
            }));
        }

        [Fact]
        public async Task Execute_Fake_CallbackExecuted()
        {
            const string testString = "testString";
            var fake = new Fake(typeof(TestClass));
            fake.Replace((TestClass t) => t.StringInstanceMethod()).Return(() => testString);
            fake.Rewrite((TestClass t) => t.StringInstanceMethod());

            Assert.Throws<NotImplementedException>(() => fake.Execute(tst => tst.Execute((TestClass t) => t.FailingMethod())));
            await Assert.ThrowsAsync<NotImplementedException>(() => fake.ExecuteAsync(tst => tst.Execute((TestClass t) => t.FailingMethodAsync())));
            Assert.Throws<NotImplementedException>(() => fake.Execute((tst, prms) =>
            {
                Assert.Equal(testString, prms.Single());
                tst.Execute((TestClass t) => t.FailingMethod());
            }));
            await Assert.ThrowsAsync<NotImplementedException>(() => fake.ExecuteAsync(async (tst, prms) =>
            {
                Assert.Equal(testString, prms.Single());
                await tst.Execute((TestClass t) => t.FailingMethodAsync());
            }));
        }
        public static IEnumerable<object[]> GetCallbacks()
            => GetFuncs().Concat(GetActions());

        public static IEnumerable<object[]> GetFuncs()
        {
            Expression<Func<TestClass, string>> instanceFunc = tst => tst.StringInstanceMethod();
            yield return new object[] { instanceFunc };
            Expression<Func<string>> staticFunc = () => TestClass.StaticStringInstanceMethod();
            yield return new object[] { staticFunc };
        }

        public static IEnumerable<object[]> GetActions()
        {
            Expression<Action<TestClass>> instanceAction = tst => tst.VoidInstanceMethod();
            yield return new object[] { instanceAction };
            Expression<Action> staticAction = () => TestClass.StaticVoidInstanceMethod();
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
