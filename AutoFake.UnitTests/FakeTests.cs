using System;
using System.Collections.Generic;
using System.Linq;
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
              && Equals(setup1.SetupArguments, setup2.SetupArguments);

        private bool Equals(List<FakeArgument> arguments1, List<FakeArgument> arguments2)
        {
            const int valueForChecking = 1;
            return arguments1.Select(a => a.Check(valueForChecking))
                .SequenceEqual(arguments2.Select(a => a.Check(valueForChecking)));
        }

        private static FakeArgument GetFakeArgument(dynamic value)
            => new FakeArgument(new EqualityArgumentChecker(value)); 

        public static IEnumerable<object[]> GetReplaceTestData()
        {
            var type = typeof(FakeTests);

            yield return new object[]
            {
                new Fake(type).Replace((FakeTests f) => f.SomeMethod(1)), new FakeSetupPack()
                {
                    Method = type.GetMethod(nameof(SomeMethod)),
                    SetupArguments = new List<FakeArgument> { GetFakeArgument(1)}
                }
            };

            yield return new object[]
            {
                new Fake(type).Replace(() => FakeTests.SomeStaticMethod(1)), new FakeSetupPack()
                {
                    Method = type.GetMethod(nameof(SomeStaticMethod)),
                    SetupArguments = new List<FakeArgument> { GetFakeArgument(1)}
                }
            };

            yield return new object[]
            {
                new Fake(type).Replace((FakeTests f) => f.SomeVoidMethod()), new FakeSetupPack()
                {
                    IsVoid = true,
                    Method = type.GetMethod(nameof(SomeVoidMethod)),
                    SetupArguments = new List<FakeArgument>()
                }
            };

            yield return new object[]
            {
                new Fake(type).Replace(() => FakeTests.SomeStaticVoidMethod()), new FakeSetupPack()
                {
                    IsVoid = true,
                    Method = type.GetMethod(nameof(SomeStaticVoidMethod)),
                    SetupArguments = new List<FakeArgument>()
                }
            };

            yield return new object[]
            {
                new Fake(type).Replace((FakeTests f) => f.SomeProperty), new FakeSetupPack()
                {
                    Method = type.GetProperty(nameof(SomeProperty)).GetMethod,
                    SetupArguments = new List<FakeArgument>()
                }
            };

            yield return new object[]
            {
                new Fake(type).Replace(() => FakeTests.SomeStaticProperty), new FakeSetupPack()
                {
                    Method = type.GetProperty(nameof(SomeStaticProperty)).GetMethod,
                    SetupArguments = new List<FakeArgument>()
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
                    SetupArguments = new List<FakeArgument> { GetFakeArgument(1)}
                }
            };

            yield return new object[]
            {
                new Fake(type).Verify(() => FakeTests.SomeStaticMethod(1)), new FakeSetupPack()
                {
                    IsVerification = true,
                    Method = type.GetMethod(nameof(SomeStaticMethod)),
                    SetupArguments = new List<FakeArgument> { GetFakeArgument(1)}
                }
            };

            yield return new object[]
            {
                new Fake(type).Verify((FakeTests f) => f.SomeVoidMethod()), new FakeSetupPack()
                {
                    IsVerification = true,
                    IsVoid = true,
                    Method = type.GetMethod(nameof(SomeVoidMethod)),
                    SetupArguments = new List<FakeArgument>()
                }
            };

            yield return new object[]
            {
                new Fake(type).Verify(() => FakeTests.SomeStaticVoidMethod()), new FakeSetupPack()
                {
                    IsVerification = true,
                    IsVoid = true,
                    Method = type.GetMethod(nameof(SomeStaticVoidMethod)),
                    SetupArguments = new List<FakeArgument>()
                }
            };

            yield return new object[]
            {
                new Fake(type).Verify((FakeTests f) => f.SomeProperty), new FakeSetupPack()
                {
                    IsVerification = true,
                    Method = type.GetProperty(nameof(SomeProperty)).GetMethod,
                    SetupArguments = new List<FakeArgument>()
                }
            };

            yield return new object[]
            {
                new Fake(type).Verify(() => FakeTests.SomeStaticProperty), new FakeSetupPack()
                {
                    IsVerification = true,
                    Method = type.GetProperty(nameof(SomeStaticProperty)).GetMethod,
                    SetupArguments = new List<FakeArgument>()
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
    }
}
