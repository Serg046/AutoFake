using System;

namespace AutoFake.Abstractions.Setup.Mocks;

internal interface ISourceMemberInsertMock : ISourceMemberMock
{
	Action Closure { get; }
}
