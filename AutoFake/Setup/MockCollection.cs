using AutoFake.Setup.Mocks;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace AutoFake.Setup
{
    internal class MockCollection : IEnumerable<MockCollection.Item>
    {
        private readonly List<Item> _mocks = new List<Item>();

        public int Count => _mocks.Count;

        public void Add(MethodBase method, ICollection<IMock> mocks) => _mocks.Add(new Item(method, mocks));

        public IEnumerator<Item> GetEnumerator() => _mocks.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public class Item
        {
            public Item(MethodBase method, ICollection<IMock> mocks)
            {
                Method = method;
                Mocks = mocks;
            }

            public MethodBase Method { get; }
            public ICollection<IMock> Mocks { get; set; }
        }
    }
}
