using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AutoFake.Exceptions;
using AutoFixture.Xunit2;
using Mono.Cecil;
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

            fake.Rewrite(callback);

            Assert.NotNull(fake.Mocks.Single().Method);
        }

        [Theory]
        [MemberData(nameof(GetCallbacks))]
        public void Rewrite_GenericFake_Throws(dynamic callback)
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(callback);

            Assert.NotNull(fake.Mocks.Single().Method);
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

            var assembly = AssemblyDefinition.ReadAssembly(fileName);
            var savedType = assembly.MainModule.GetType(type.FullName, true).Resolve();
            Assert.Contains(savedType.Methods, m => m.Name == nameof(TestClass.VoidInstanceMethod));
            // TODO: Fix this
            //File.Delete(fileName);
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
            fake.Rewrite(f => f.StringInstanceMethod())
                .Replace(f => f.StringInstanceMethod()).Return(() => testString);

            Assert.Throws<NotImplementedException>(() => fake.Execute(() => TestClass.FailingStaticMethod()));
            Assert.Throws<NotImplementedException>(() => fake.Execute(tst => tst.FailingMethod()));

            await Assert.ThrowsAsync<NotImplementedException>(() => fake.ExecuteAsync(() => TestClass.FailingStaticMethodAsync()));
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
            fake.Rewrite((TestClass t) => t.StringInstanceMethod())
                .Replace((TestClass t) => t.StringInstanceMethod()).Return(() => testString);

            Assert.Throws<NotImplementedException>(() => fake.Execute(() => TestClass.FailingStaticMethod()));
            Assert.Throws<NotImplementedException>(() => fake.Execute(tst => tst.Execute((TestClass t) => t.FailingMethod())));
            
            await Assert.ThrowsAsync<NotImplementedException>(() => fake.ExecuteAsync(() => TestClass.FailingStaticMethodAsync()));
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

        [Fact]
        public void Execute_MultipleCapturedVarWithTheSameType_Throws()
        {
            DateTime d1 = DateTime.Now, d2 = DateTime.Now;
            var fake = new Fake<TestClass>();

            Assert.Throws<InitializationException>(() => fake.Execute(() => Console.WriteLine(d2 - d1)));
        }

        [Fact]
        public void Execute_CapturedVarWithoutMocks_Throws()
        {
            var d1 = DateTime.Now;
            var fake = new Fake<TestClass>();

            Assert.Throws<InitializationException>(() => fake.Execute(() => Console.WriteLine(d1)));
        }

        [Fact]
        public void Execute_CapturedVarWith2Mocks_Throws()
        {
            var d1 = new DateTime(2020, 5, 27);
            var fake = new Fake<TestClass>();

            fake.Rewrite(f => f.SomeMethod()).Replace(() => DateTime.Now).Return(d1);
            fake.Rewrite(f => f.SomeMethod()).Replace(() => DateTime.Now).Return(d1);

            Assert.Throws<InitializationException>(() => fake.Execute(() => Console.WriteLine(d1)));
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
            public void SomeMethod() { }

            internal static void FailingStaticMethod() => throw new NotImplementedException();
            internal void FailingMethod() => throw new NotImplementedException();
            internal void VoidInstanceMethod() { }
            internal string StringInstanceMethod() => string.Empty;
            internal static void StaticVoidInstanceMethod() { }
            internal static string StaticStringInstanceMethod() => string.Empty;

            internal Task FailingMethodAsync() => FailingStaticMethodAsync();
            internal static async Task FailingStaticMethodAsync()
            {
                await Task.Delay(1);
                throw new NotImplementedException();
            }
        }
    }
}
