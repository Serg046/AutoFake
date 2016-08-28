using System;
using System.Linq.Expressions;
using AutoFake.Exceptions;
using GuardExtensions;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeTests : ExpressionUnitTest
    {
        [Fact]
        public void Ctor_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>(null));
        }

        [Fact]
        public void Setup_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>().Setup((Expression<Func<bool>>)null));
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>().Setup((Expression<Func<int, bool>>)null));
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>().Setup((Expression<Action<int>>)null));
        }

        [Fact]
        public void Setup_PropertyCallExpression_Success()
        {
            var fake = new Fake<SomeType>();
            fake.Setup(() => SomeProperty);

            Assert.Equal(1, fake.Setups.Count);
            Assert.Equal(GetProperty(nameof(SomeProperty)).GetMethod, fake.Setups[0].Method);
            Assert.Equal(0, fake.Setups[0].SetupArguments.Length);
            Assert.Equal(-1, fake.Setups[0].ExpectedCallsCount);
        }

        [Fact]
        public void Setup_MethodCallExpression_Success()
        {
            var fake = new Fake<SomeType>();
            fake.Setup((SomeType someType) => SomeMethod(0));

            Assert.Equal(1, fake.Setups.Count);
            Assert.Equal(GetMethod(nameof(SomeMethod), new [] {typeof(object)}), fake.Setups[0].Method);
            Assert.Equal(1, fake.Setups[0].SetupArguments.Length);
            Assert.Equal(0, fake.Setups[0].SetupArguments[0]);
            Assert.Equal(-1, fake.Setups[0].ExpectedCallsCount);
        }

        [Fact]
        public void Setup_VoidMethodCallExpression_Success()
        {
            var fake = new Fake<SomeType>();
            fake.Setup((SomeType someType) => SomeMethod());

            Assert.Equal(1, fake.Setups.Count);
            Assert.Equal(GetMethod(nameof(SomeMethod)), fake.Setups[0].Method);
            Assert.Equal(0, fake.Setups[0].SetupArguments.Length);
            Assert.Equal(-1, fake.Setups[0].ExpectedCallsCount);
        }

        [Fact]
        public void SaveFakeAssembly_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>().SaveFakeAssembly(null));
        }

        [Fact]
        public void Execute_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>().Execute((Expression<Func<FakeTests, bool>>)null));
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>().Execute((Expression<Action<FakeTests>>)null));
        }

        [Fact]
        public void Execute_NoSetups_Throws()
        {
            Assert.Throws<FakeGeneretingException>(() => new Fake<FakeTests>().Execute(f => f.Execute_Null_Throws()));
        }

        //please find more tests for Execute() in the integration tests
    }
}
