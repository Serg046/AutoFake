﻿using System;
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
        public void Replace_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Fake<FakeTests>().Replace(null));
        }

        [Fact]
        public void Verify_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Fake<FakeTests>().Verify(null));
        }

        [Fact]
        public void Execute_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Fake<FakeTests>().Execute(null));
        }

        [Fact]
        public void Reset_ClearsSetups()
        {
            var fake = new Fake<FakeTests>();

            fake.Verify((FakeTests f) => f.Reset_ClearsSetups())
                .ExpectedCallsCount(1);
            Assert.NotEmpty(fake.Mocks);

            fake.Reset();
            Assert.Empty(fake.Mocks);
        }

        [Fact]
        public void Rewrite_AfterExecuteInvocation_Throws()
        {
            var fake = new Fake<TestClass>();
            fake.Execute(f => f.SomeMethod());

            Assert.Throws<FakeGeneretingException>(() => fake.Rewrite(f => f.SomeMethod()));
        }

        [Fact]
        public void Execute_ConstructorAfterExecuteInvocation_Throws()
        {
            var fake = new Fake<TestClass>();
            fake.Execute();

            Assert.Throws<InvalidOperationException>(() => fake.Execute());
        }

        private class TestClass
        {
            public void SomeMethod()
            {
            }
        }
    }
}
