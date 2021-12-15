using AutoFake.Setup.Mocks;
using System.Collections;
using System.Collections.Generic;
using AutoFake.Expression;

namespace AutoFake.Setup
{
    internal class MockCollection : IEnumerable<MockCollection.Item>
    {
        private readonly List<Item> _mocks = new();

        public int Count => _mocks.Count;

        public void Add(IInvocationExpression invocationExpression, ICollection<IMock> mocks)
	        => _mocks.Add(new Item(invocationExpression, mocks));

        public IEnumerator<Item> GetEnumerator() => _mocks.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public class Item
        {
            public Item(IInvocationExpression invocationExpression, ICollection<IMock> mocks)
            {
                InvocationExpression = invocationExpression;
                Mocks = mocks;
            }

            public IInvocationExpression InvocationExpression { get; }
            public ICollection<IMock> Mocks { get; set; }
        }
    }
}
