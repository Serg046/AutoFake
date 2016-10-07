using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoFake.Exceptions;
using AutoFake.Setup;
using GuardExtensions;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeTests
    {
        public int SomeMethod(int a) => a;
        public static int SomeStaticMethod(int a) => a;
        public void SomeVoidMethod() { }
        public static void SomeStaticVoidMethod() { }
        public int SomeProperty => 1;
        public static int SomeStaticProperty => 1;
        public int SomeField = 0;
        public static int SomeStaticField = 0;

        private class TestClass
        {
            public TestClass(StringBuilder dependency1, StringBuilder dependency2)
            {
            }

            public void TestMethod()
            {
            }
        }

        internal class InternalTestClass
        {
            internal InternalTestClass()
            {
            }
        }

        protected class ProtectedTestClass
        {
            protected ProtectedTestClass()
            {
            }
        }

        private class PrivateTestClass
        {
            private PrivateTestClass()
            {
            }
        }

        [Fact]
        public void Ctor_NullAsDependency_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new Fake<TestClass>(null));
            Assert.Throws<ContractFailedException>(() => new Fake<TestClass>(new object[] { null }));
            Assert.Throws<ContractFailedException>(() => new Fake<TestClass>(null, null));
            Assert.Throws<ContractFailedException>(() => new Fake<TestClass>(new StringBuilder(), null));
            Assert.Throws<ContractFailedException>(() => new Fake<TestClass>(null, new StringBuilder()));
            new Fake<TestClass>(FakeDependency.Null<StringBuilder>(), FakeDependency.Null<StringBuilder>());
        }

        [Fact]
        public void Ctor_InvalidInput_Throws()
        {
            Assert.Throws<FakeGeneretingException>(() 
                => new Fake<TestClass>(new StringBuilder()).Execute(t => t.TestMethod()));
            Assert.Throws<FakeGeneretingException>(()
                => new Fake<TestClass>(new StringBuilder(), new StringBuilder(), new StringBuilder()).Execute(t => t.TestMethod()));
            Assert.Throws<FakeGeneretingException>(()
                => new Fake<TestClass>(1, 1).Execute(t => t.TestMethod()));
            new Fake<TestClass>(new StringBuilder(), new StringBuilder()).Execute(t => t.TestMethod());
        }

        [Fact]
        public void Ctor_NonPublicConstructor_Success()
        {
            new Fake<InternalTestClass>().Execute(t => t.ToString());
            new Fake<ProtectedTestClass>().Execute(t => t.ToString());
            new Fake<PrivateTestClass>().Execute(t => t.ToString());
        }

        [Fact]
        public void SaveFakeAssembly_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>().SaveFakeAssembly(null));
        }

        private bool Equals(FakeSetupPack setup1, FakeSetupPack setup2)
           => setup1.IsVerification == setup2.IsVerification
              && setup1.IsVoid == setup2.IsVoid
              && setup1.Method == setup2.Method
              && setup1.ReturnObject == setup2.ReturnObject
              && setup1.SetupArguments.SequenceEqual(setup2.SetupArguments);

        public static IEnumerable<object[]> GetReplaceTestData()
        {
            var type = typeof(FakeTests);

            yield return new object[]
            {
                new Fake(type).Replace((FakeTests f) => f.SomeMethod(1)), new FakeSetupPack()
                {
                    Method = type.GetMethod(nameof(SomeMethod)),
                    SetupArguments = new object[] {1}
                }
            };

            yield return new object[]
            {
                new Fake(type).Replace(() => FakeTests.SomeStaticMethod(1)), new FakeSetupPack()
                {
                    Method = type.GetMethod(nameof(SomeStaticMethod)),
                    SetupArguments = new object[] {1}
                }
            };

            yield return new object[]
            {
                new Fake(type).Replace((FakeTests f) => f.SomeVoidMethod()), new FakeSetupPack()
                {
                    IsVoid = true,
                    Method = type.GetMethod(nameof(SomeVoidMethod)),
                    SetupArguments = new object[0]
                }
            };

            yield return new object[]
            {
                new Fake(type).Replace(() => FakeTests.SomeStaticVoidMethod()), new FakeSetupPack()
                {
                    IsVoid = true,
                    Method = type.GetMethod(nameof(SomeStaticVoidMethod)),
                    SetupArguments = new object[0]
                }
            };

            yield return new object[]
            {
                new Fake(type).Replace((FakeTests f) => f.SomeProperty), new FakeSetupPack()
                {
                    Method = type.GetProperty(nameof(SomeProperty)).GetMethod,
                    SetupArguments = new object[0]
                }
            };

            yield return new object[]
            {
                new Fake(type).Replace(() => FakeTests.SomeStaticProperty), new FakeSetupPack()
                {
                    Method = type.GetProperty(nameof(SomeStaticProperty)).GetMethod,
                    SetupArguments = new object[0]
                }
            };
        }

        [Theory]
        [MemberData(nameof(GetReplaceTestData))]
        internal void Replace_ProvidesAbilityToMock(MockInstaller mockInstaller, FakeSetupPack expectedSetup)
        {
            Assert.True(Equals(expectedSetup, mockInstaller.FakeSetupPack));
        }

        [Fact]
        public void Replace_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>().Replace(null));
        }

        public static IEnumerable<object[]> GetVerifyTestData()
        {
            var type = typeof(FakeTests);

            yield return new object[]
            {
                new Fake(type).Verify((FakeTests f) => f.SomeMethod(1)), new FakeSetupPack()
                {
                    IsVerification = true,
                    Method = type.GetMethod(nameof(SomeMethod)),
                    SetupArguments = new object[] {1}
                }
            };

            yield return new object[]
            {
                new Fake(type).Verify(() => FakeTests.SomeStaticMethod(1)), new FakeSetupPack()
                {
                    IsVerification = true,
                    Method = type.GetMethod(nameof(SomeStaticMethod)),
                    SetupArguments = new object[] {1}
                }
            };

            yield return new object[]
            {
                new Fake(type).Verify((FakeTests f) => f.SomeVoidMethod()), new FakeSetupPack()
                {
                    IsVerification = true,
                    IsVoid = true,
                    Method = type.GetMethod(nameof(SomeVoidMethod)),
                    SetupArguments = new object[0]
                }
            };

            yield return new object[]
            {
                new Fake(type).Verify(() => FakeTests.SomeStaticVoidMethod()), new FakeSetupPack()
                {
                    IsVerification = true,
                    IsVoid = true,
                    Method = type.GetMethod(nameof(SomeStaticVoidMethod)),
                    SetupArguments = new object[0]
                }
            };

            yield return new object[]
            {
                new Fake(type).Verify((FakeTests f) => f.SomeProperty), new FakeSetupPack()
                {
                    IsVerification = true,
                    Method = type.GetProperty(nameof(SomeProperty)).GetMethod,
                    SetupArguments = new object[0]
                }
            };

            yield return new object[]
            {
                new Fake(type).Verify(() => FakeTests.SomeStaticProperty), new FakeSetupPack()
                {
                    IsVerification = true,
                    Method = type.GetProperty(nameof(SomeStaticProperty)).GetMethod,
                    SetupArguments = new object[0]
                }
            };
        }

        [Theory]
        [MemberData(nameof(GetVerifyTestData))]
        internal void Verify_ProvidesAbilityToMock(MockInstaller mockInstaller, FakeSetupPack expectedSetup)
        {
            Assert.True(Equals(expectedSetup, mockInstaller.FakeSetupPack));
        }

        [Fact]
        public void Verify_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>().Verify(null));
        }

        [Fact]
        public void Execute_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>().Execute(null));
        }

        [Fact]
        public void Execute_SecondInvocation_Throws()
        {
            var fake = new Fake<FakeTests>();
            fake.Execute(f => f.SomeVoidMethod());
            Assert.Throws<InvalidOperationException>(() => fake.Execute(f => f.SomeVoidMethod()));
        }

        [Fact]
        public void ResetSetups_ResetsState()
        {
            var fake = new Fake<FakeTests>();
            fake.Execute(f => f.SomeVoidMethod());
            fake.ResetSetups();
            fake.Execute(f => f.SomeVoidMethod());
        }

        [Fact]
        public void Execute_ProvidesAbilityToExecute()
        {
            var fake = new Fake<FakeTests>();
            fake.Execute(f => f.SomeMethod(1));

            fake.ResetSetups();
            fake.Execute(() => FakeTests.SomeStaticMethod(1));

            fake.ResetSetups();
            fake.Execute(f => f.SomeVoidMethod());

            fake.ResetSetups();
            fake.Execute(() => FakeTests.SomeStaticVoidMethod());

            fake.ResetSetups();
            fake.Execute(f => f.SomeProperty);

            fake.ResetSetups();
            fake.Execute(() => FakeTests.SomeStaticProperty);
        }

        [Fact]
        public void GetStateValue_InvalidInput_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new Fake<FakeTests>().GetStateValue<int>(null));
            Assert.Throws<InvalidOperationException>(() => new Fake<FakeTests>().GetStateValue(f => f.SomeProperty));
        }

        public void TestMethod()
        {
        }

        [Fact]
        public void GetStateValue_ProvidesAbilityToExecute()
        {
            var fake = new Fake<FakeTests>();
            fake.Execute(f => f.TestMethod());

            fake.GetStateValue(f => f.SomeMethod(1));
            fake.GetStateValue(() => FakeTests.SomeStaticMethod(1));
            fake.GetStateValue(f => f.SomeProperty);
            fake.GetStateValue(() => FakeTests.SomeStaticProperty);
            fake.GetStateValue(f => f.SomeField);
            fake.GetStateValue(() => FakeTests.SomeStaticField);
        }
    }
}
