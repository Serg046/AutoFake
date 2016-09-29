using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardExtensions;

namespace AutoFake.Setup
{
    internal class SetupCollection : IEnumerable<FakeSetupPack>
    {
        private readonly List<FakeSetupPack> _setups;

        public SetupCollection()
        {
            _setups = new List<FakeSetupPack>();
        }

        public int Count => _setups.Count;

        public void Add(FakeSetupPack setup)
        {
            Guard.IsNotNull(setup);
            Guard.IsNotNull(setup.Method);

            var count = _setups.Count(s => s.Method.Name == setup.Method.Name);
            setup.ReturnObjectFieldName = count > 0
                ? setup.Method.Name + count
                : setup.Method.Name;

            _setups.Add(setup);
        }

        public IEnumerator<FakeSetupPack> GetEnumerator() => _setups.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Clear() => _setups.Clear();
    }
}
