using System.Collections.Generic;
using AutoFake.Abstractions.Setup.Mocks;

namespace AutoFake.Abstractions.Setup
{
	internal interface IMockCollection : IEnumerable<IMock>
	{
		IMock this[int index] { get; set; }
		int Count { get; }
		void Add(IMock mock);
	}
}