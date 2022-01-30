using AutoFake.Setup.Mocks;

namespace AutoFake.Abstractions.Setup.Mocks
{
	internal interface ISourceMemberMock : IMock
	{
        SourceMemberMetaData SourceMemberMetaData { get; }
	}
}
