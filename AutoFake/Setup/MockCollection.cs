using System.Collections;
using AutoFake.Setup.Mocks;
using System.Collections.Generic;

namespace AutoFake.Setup
{
    internal class MockCollection : IMockCollection
    {
	    private readonly List<IMock> _mocks = new();

	    public IMock this[int index]
	    {
		    get => _mocks[index];
		    set => _mocks[index] = value;
	    }

		public int Count => _mocks.Count;

	    public void Add(IMock mock) => _mocks.Add(mock);

	    public IEnumerator<IMock> GetEnumerator() => _mocks.GetEnumerator();

	    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
