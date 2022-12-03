using System;

namespace AutoFake.Abstractions.Setup.Mocks;

public interface ISourceMemberInsertMock : ISourceMemberMock
{
	Action Closure { get; }
}
