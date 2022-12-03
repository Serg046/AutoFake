namespace AutoFake.Abstractions.Setup.Mocks;

public interface IReplaceMock : ISourceMemberMock
{
	object? ReturnObject { get; set; }
}
