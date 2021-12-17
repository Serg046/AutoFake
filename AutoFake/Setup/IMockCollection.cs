using System.Collections.Generic;
using AutoFake.Setup.Mocks;

namespace AutoFake.Setup
{
	internal interface IMockCollection : IEnumerable<IMock>
	{
		IMock this[int index] { get; set; }
		int Count { get; }
		void Add(IMock mock);
	}
}