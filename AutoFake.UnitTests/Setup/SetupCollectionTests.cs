using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFake.Setup;
using GuardExtensions;
using Xunit;

namespace AutoFake.UnitTests.Setup
{
    public class SetupCollectionTests
    {
        private void SomeMethod()
        {
        }

        private int SomeMethod(int i) => 0;

        private IEnumerable<MethodInfo> GetMethods(string name)
            => GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(m => m.Name == name);

        private FakeSetupPack GetFakeSetupPack()
        {
            var method = GetMethods(nameof(SomeMethod)).First();
            return new FakeSetupPack() {Method = method};
        }

        private readonly SetupCollection _setups;

        public SetupCollectionTests()
        {
            _setups = new SetupCollection();
        }

        [Fact]
        public void Add_Setup_Added()
        {
            _setups.Add(GetFakeSetupPack());

            Assert.Equal(1, _setups.Count());
        }

        [Fact]
        public void Add_Setup_FieldNameSet()
        {
            _setups.Add(GetFakeSetupPack());

            var setups = _setups.ToList();

            Assert.Equal("SomeMethod", setups[0].ReturnObjectFieldName);
        }

        [Fact]
        public void Add_TwoSimilarMethods_DifferentFieldNames()
        {
            var methods = GetMethods(nameof(SomeMethod)).ToList();

            _setups.Add(new FakeSetupPack() { Method = methods[0] });
            _setups.Add(new FakeSetupPack() { Method = methods[1] });

            var setups = _setups.ToList();

            Assert.Equal("SomeMethod", setups[0].ReturnObjectFieldName);
            Assert.Equal("SomeMethod1", setups[1].ReturnObjectFieldName);
        }

        [Fact]
        public void Count_ReturnsCount()
        {
            _setups.Add(GetFakeSetupPack());

            Assert.Equal(1, _setups.Count);
        }

        [Fact]
        public void Clear_ResetsCollection()
        {
            _setups.Add(GetFakeSetupPack());
            _setups.Add(GetFakeSetupPack());

            _setups.Clear();

            Assert.Empty(_setups);
        }
    }
}
