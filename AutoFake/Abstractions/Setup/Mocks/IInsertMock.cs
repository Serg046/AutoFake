using System;

namespace AutoFake.Abstractions.Setup.Mocks;

public interface IInsertMock : IMock
{
	Action Closure { get; }

	public enum Location
	{
		Before,
		After
	}
}
