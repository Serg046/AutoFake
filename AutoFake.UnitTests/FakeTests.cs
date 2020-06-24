using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
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
            var fake = new Fake<TestClass>();

            Assert.Throws<NotImplementedException>(() => fake.Execute(t => t.FailingMethod()));
        }

        [Theory]
        [MemberData(nameof(GetCallbacks))]
        public void Execute_GenericFake_CallbackExecuted(dynamic callback)
        {
            var fake = new Fake<TestClass>();

            Action act = () => fake.Execute(callback);
            Assert.Throws<NotImplementedException>(act);
        }

        [Theory]
        [MemberData(nameof(GetCallbacks))]
        public void Execute_Fake_CallbackExecuted(dynamic callback)
        {
            var fake = new Fake(typeof(TestClass));

            Action act = () => fake.Execute(callback);
            Assert.Throws<NotImplementedException>(act);
        }

        [Fact]
        public void Execute_GenericFakeWithNull_Throws()
        {
            var fake = new Fake<TestClass>();

            Assert.Throws<ArgumentNullException>(() => fake.Execute((Expression<Func<TestClass, string>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Execute((Expression<Func<string>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Execute((Expression<Action<TestClass>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Execute((Expression<Action>)null));
        }

        [Fact]
        public void Execute_FakeWithNull_Throws()
        {
            var fake = new Fake(typeof(TestClass));

            Assert.Throws<ArgumentNullException>(() => fake.Execute((Expression<Func<TestClass, string>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Execute((Expression<Func<string>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Execute((Expression<Action<TestClass>>)null));
            Assert.Throws<ArgumentNullException>(() => fake.Execute((Expression<Action>)null));
        }

        public static IEnumerable<object[]> GetCallbacks()
            => GetFuncs().Concat(GetActions());

        public static IEnumerable<object[]> GetFuncs()
        {
            Expression<Func<TestClass, string>> instanceFunc = tst => tst.FailingMethod();
            yield return new object[] { instanceFunc };
            Expression<Func<string>> staticFunc = () => TestClass.FailingStaticMethod();
            yield return new object[] { staticFunc };
        }

        public static IEnumerable<object[]> GetActions()
        {
            Expression<Action<TestClass>> instanceAction = tst => tst.FailingVoidMethod();
            yield return new object[] { instanceAction };
            Expression<Action> staticAction = () => TestClass.FailingVoidStaticMethod();
            yield return new object[] { staticAction };
        }

        internal class TestClass
        {
            public void SomeMethod() { }

            internal static string FailingStaticMethod() => throw new NotImplementedException();
            internal string FailingMethod() => throw new NotImplementedException();
            internal static void FailingVoidStaticMethod() => throw new NotImplementedException();
            internal void FailingVoidMethod() => throw new NotImplementedException();
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
