using System.Collections.Generic;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Mocks;

namespace AutoFake.Setup
{
    internal class MockCollection : IMockCollection
    {
		public IList<IMock> Mocks { get; } = new List<IMock>();
        public ISet<IMock> ContractMocks { get; } = new HashSet<IMock>();
    }
}
