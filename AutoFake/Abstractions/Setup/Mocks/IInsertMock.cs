using System;

namespace AutoFake.Abstractions.Setup.Mocks;

internal interface IInsertMock : IMock
{
	Action Closure { get; }
}
