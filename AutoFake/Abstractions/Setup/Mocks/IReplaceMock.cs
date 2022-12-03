namespace AutoFake.Abstractions.Setup.Mocks;

internal interface IReplaceMock : ISourceMemberMock
{
	object? ReturnObject { get; set; }
}
