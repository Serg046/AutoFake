using AutoFake.Abstractions.Setup.Mocks;
using System.Collections.Generic;

namespace AutoFake.Abstractions.Setup
{
	internal interface IMockCollection
	{
		IList<IMock> Mocks { get; }
		ISet<IMockInjector> ContractMocks { get; }
	}
}
