﻿using System;
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
        [Theory]
        [MemberData(nameof(GetCallbacks))]
        internal void Replace_Fake_MockAdded(dynamic callback)
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(callback);

            Assert.NotEmpty(fake.Mocks);
        }

        [Theory]
        [MemberData(nameof(GetCallbacks))]
        internal void Replace_GenericFake_MockAdded(dynamic callback)
        {
            var fake = new Fake<TestClass>();

            fake.Replace(callback);

            Assert.NotEmpty(fake.Mocks);
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
        public void Rewrite_AfterExecuteInvocation_Throws()
        {
            var fake = new Fake<TestClass>();
            fake.Execute(f => f.VoidInstanceMethod());

            Assert.Throws<FakeGeneretingException>(() => fake.Rewrite(f => f.VoidInstanceMethod()));
        }

        [Theory]
        [MemberData(nameof(GetCallbacks))]
        public void Rewrite_Fake_Throws(dynamic callback)
        {
            var fake = new Fake(typeof(TestClass));
            var mock = new Mock<IMock>();
            fake.Mocks.Add(mock.Object);

            fake.Rewrite(callback);

            mock.Verify(m => m.PrepareForInjecting(It.IsAny<IMocker>()));
        }

        [Theory]
        [MemberData(nameof(GetCallbacks))]
        public void Rewrite_GenericFake_Throws(dynamic callback)
        {
            var fake = new Fake<TestClass>();
            var mock = new Mock<IMock>();
            fake.Mocks.Add(mock.Object);

            fake.Rewrite(callback);

            mock.Verify(m => m.PrepareForInjecting(It.IsAny<IMocker>()));
        }

        [Fact]
        public void Reset_ClearsSetups()
        {
            var fake = new Fake<FakeTests>();

            fake.Verify(f => f.Reset_ClearsSetups())
                .ExpectedCallsCount(1);
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
            fake.Replace(f => f.StringInstanceMethod()).Returns(() => testString);
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
            fake.Replace((TestClass t) => t.StringInstanceMethod()).Returns(() => testString);
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
        {
            Expression<Func<TestClass, string>> instanceFunc = tst => tst.StringInstanceMethod();
            yield return new object[] { instanceFunc };
            Expression<Action<TestClass>> instanceAction = tst => tst.VoidInstanceMethod();
            yield return new object[] { instanceAction };
            Expression<Func<string>> staticFunc = () => TestClass.StaticStringInstanceMethod();
            yield return new object[] { staticFunc };
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
