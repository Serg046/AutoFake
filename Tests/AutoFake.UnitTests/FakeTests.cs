using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoFake.Expression;
using AutoFake.Setup;
using DryIoc;
using FluentAssertions;
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

            Assert.NotNull(fake.Services.Resolve<KeyValuePair<IInvocationExpression, IMockCollection>[]>().Single().Key);
        }

        [Theory]
        [MemberData(nameof(GetCallbacks))]
        public void Rewrite_GenericFake_Throws(dynamic callback)
        {
            var fake = new Fake<TestClass>();

            fake.Rewrite(callback);
            
            Assert.NotNull(fake.Services.Resolve<KeyValuePair<IInvocationExpression, IMockCollection>[]>().Single().Key);
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

        [Theory]
        [MemberData(nameof(GetMutateCallbacks))]
        public void Execute_GenericFake_Throws(dynamic callback)
        {
            var fake = new Fake<TestClass>();

            fake.Execute(callback);

            Assert.Equal(5, fake.Execute(() => TestClass.State));
        }

        [Theory]
        [MemberData(nameof(GetMutateCallbacks))]
        public void Execute_Fake_Throws(dynamic callback)
        {
            var fake = new Fake<TestClass>();

            fake.Execute(callback);

            Assert.Equal(5, fake.Execute(() => TestClass.State));
        }

        [Fact]
        public void Ctor_DirectDependency_Success()
        {
	        const string dep = "test";
	        var fake = new Fake<StringWrapper>(dep);

	        fake.Execute(sb => sb.Value).Should().Be(dep);
        }

        [Fact]
        public void Ctor_FakeDependency_Success()
        {
            var fake = new Fake<StringWrapper>(Arg.IsNull<string>());

            fake.Execute(sb => sb.Value).Should().BeNull();
        }

        [Fact]
        public void Ctor_NullDependency_Success()
        {
	        var fake = new Fake<StringWrapper>(new object[] {null});

	        fake.Execute(sb => sb.Value).Should().BeNull();
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

        public static IEnumerable<object[]> GetMutateCallbacks()
            => GetMutateFuncs().Concat(GetMutateActions());

        public static IEnumerable<object[]> GetMutateFuncs()
        {
            Expression<Func<TestClass, string>> instanceFunc = tst => tst.FuncMutator();
            yield return new object[] { instanceFunc };
            Expression<Func<string>> staticFunc = () => TestClass.StaticFuncMutator();
            yield return new object[] { staticFunc };
        }

        public static IEnumerable<object[]> GetMutateActions()
        {
            Expression<Action<TestClass>> instanceAction = tst => tst.ActionMutator();
            yield return new object[] { instanceAction };
            Expression<Action> staticAction = () => TestClass.StaticActionMutator();
            yield return new object[] { staticAction };
        }

        internal class TestClass
        {
            public static int State;
            public void SomeMethod() { }

            internal static string FailingStaticMethod() => throw new NotImplementedException();
            internal string FailingMethod() => throw new NotImplementedException();
            internal static void FailingVoidStaticMethod() => throw new NotImplementedException();
            internal void FailingVoidMethod() => throw new NotImplementedException();

            internal void VoidInstanceMethod() { }
            internal string StringInstanceMethod() => string.Empty;
            internal static void StaticVoidInstanceMethod() { }
            internal static string StaticStringInstanceMethod() => string.Empty;

            internal static string StaticFuncMutator()
            {
                State = 5;
                return "";
            }

            internal string FuncMutator()
            {
                State = 5;
                return "";
            }

            internal static void StaticActionMutator() => State = 5;
            internal void ActionMutator() => State = 5;

            internal Task FailingMethodAsync() => FailingStaticMethodAsync();
            internal static async Task FailingStaticMethodAsync()
            {
                await Task.Delay(1);
                throw new NotImplementedException();
            }
        }

        private class StringWrapper
        {
	        public StringWrapper(string value) => Value = value;

	        public string Value { get; }
        }
    }
}
