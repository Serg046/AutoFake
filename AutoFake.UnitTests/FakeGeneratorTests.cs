using System;
using System.Collections.Generic;
using System.Linq;
using GuardExtensions;
using Xunit;

namespace AutoFake.UnitTests
{
    public class FakeGeneratorTests : ExpressionUnitTest
    {
        [Fact]
        public void Ctor_Null_Throws()
        {
            Assert.Throws<ContractFailedException>(() => new FakeGenerator(null));
        }

        [Fact]
        public void Generate_IncorrectInputValues_Throws()
        {
            var typeInfo = new TypeInfo(null, null);
            var fakeGen = new FakeGenerator(typeInfo);

            var someMethodInfo = GetType().GetMethods()[0];
            if (someMethodInfo == null)
                throw new InvalidOperationException("MethodInfo is not found");

            var setups = new List<FakeSetupPack>();
            setups.Add(new FakeSetupPack());

            Assert.Throws<ContractFailedException>(() => fakeGen.Generate(null, someMethodInfo));
            Assert.Throws<ContractFailedException>(() => fakeGen.Generate(Enumerable.Empty<FakeSetupPack>().ToList(), someMethodInfo));
            Assert.Throws<ContractFailedException>(() => fakeGen.Generate(setups, null));
        }

        [Fact]
        public void Save_Null_Throws()
        {
            var typeInfo = new TypeInfo(null, null);
            var fakeGen = new FakeGenerator(typeInfo);
            Assert.Throws<ContractFailedException>(() => fakeGen.Save(null));
        }
    }
}
