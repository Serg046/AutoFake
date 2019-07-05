using System;
using System.Linq.Expressions;
using AutoFake.Exceptions;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeTests
    {
        [Fact]
        public void SaveFakeAssembly_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Fake<FakeTests>().SaveFakeAssembly(null));
        }

        [Fact]
        internal void Replace_Null_Throws()
        {
            Expression<Func<TestClass, int>> instanceSetupFunc = null;
            Assert.Throws<ArgumentNullException>(() => new Fake<TestClass>().Replace(instanceSetupFunc));
            Assert.Throws<ArgumentNullException>(() => new Fake(typeof(TestClass)).Replace(instanceSetupFunc));

            Expression<Action<TestClass>> voidInstanceSetupFunc = null;
            Assert.Throws<ArgumentNullException>(() => new Fake<TestClass>().Replace(voidInstanceSetupFunc));
            Assert.Throws<ArgumentNullException>(() => new Fake(typeof(TestClass)).Replace(voidInstanceSetupFunc));

            Expression<Func<int>> staticSetupFunc = null;
            Assert.Throws<ArgumentNullException>(() => new Fake<TestClass>().Replace(staticSetupFunc));
            Assert.Throws<ArgumentNullException>(() => new Fake(typeof(TestClass)).Replace(staticSetupFunc));

            Expression<Action> voidStaticSetupFunc = null;
            Assert.Throws<ArgumentNullException>(() => new Fake<TestClass>().Replace(voidStaticSetupFunc));
            Assert.Throws<ArgumentNullException>(() => new Fake(typeof(TestClass)).Replace(voidStaticSetupFunc));

        }

        [Fact]
        public void Verify_Null_Throws()
        {
            Expression<Func<TestClass, int>> instanceSetupFunc = null;
            Assert.Throws<ArgumentNullException>(() => new Fake<TestClass>().Verify(instanceSetupFunc));
            Assert.Throws<ArgumentNullException>(() => new Fake(typeof(TestClass)).Verify(instanceSetupFunc));

            Expression<Action<TestClass>> voidInstanceSetupFunc = null;
            Assert.Throws<ArgumentNullException>(() => new Fake<TestClass>().Verify(voidInstanceSetupFunc));
            Assert.Throws<ArgumentNullException>(() => new Fake(typeof(TestClass)).Verify(voidInstanceSetupFunc));

            Expression<Func<int>> staticSetupFunc = null;
            Assert.Throws<ArgumentNullException>(() => new Fake<TestClass>().Verify(staticSetupFunc));
            Assert.Throws<ArgumentNullException>(() => new Fake(typeof(TestClass)).Verify(staticSetupFunc));

            Expression<Action> voidStaticSetupFunc = null;
            Assert.Throws<ArgumentNullException>(() => new Fake<TestClass>().Verify(voidStaticSetupFunc));
            Assert.Throws<ArgumentNullException>(() => new Fake(typeof(TestClass)).Verify(voidStaticSetupFunc));
        }

        [Fact]
        public void Rewrite_Null_Throws()
        {
            Expression<Func<TestClass, int>> instanceSetupFunc = null;
            Assert.Throws<ArgumentNullException>(() => new Fake<TestClass>().Rewrite(instanceSetupFunc));
            Assert.Throws<ArgumentNullException>(() => new Fake(typeof(TestClass)).Rewrite(instanceSetupFunc));

            Expression<Action<TestClass>> voidInstanceSetupFunc = null;
            Assert.Throws<ArgumentNullException>(() => new Fake<TestClass>().Rewrite(voidInstanceSetupFunc));
            Assert.Throws<ArgumentNullException>(() => new Fake(typeof(TestClass)).Rewrite(voidInstanceSetupFunc));

            Expression<Func<int>> staticSetupFunc = null;
            Assert.Throws<ArgumentNullException>(() => new Fake<TestClass>().Rewrite(staticSetupFunc));
            Assert.Throws<ArgumentNullException>(() => new Fake(typeof(TestClass)).Rewrite(staticSetupFunc));

            Expression<Action> voidStaticSetupFunc = null;
            Assert.Throws<ArgumentNullException>(() => new Fake<TestClass>().Rewrite(voidStaticSetupFunc));
            Assert.Throws<ArgumentNullException>(() => new Fake(typeof(TestClass)).Rewrite(voidStaticSetupFunc));
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
        public void Rewrite_AfterExecuteInvocation_Throws()
        {
            var fake = new Fake<TestClass>();
            fake.Execute(f => f.VoidInstanceMethod());

            Assert.Throws<FakeGeneretingException>(() => fake.Rewrite(f => f.VoidInstanceMethod()));
        }

        internal class TestClass
        {
            public const int CONST_FIELD = 5;

            public void VoidInstanceMethod()
            {
            }
        }
    }
}
