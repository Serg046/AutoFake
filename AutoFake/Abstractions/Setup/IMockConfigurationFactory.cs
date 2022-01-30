using System;
using AutoFake.Abstractions.Setup.Mocks;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;

namespace AutoFake.Abstractions.Setup
{
	internal interface IMockConfigurationFactory
	{
		T GetInsertMockConfiguration<T>(Action<IMock> setMock, Action closure);
		VerifyMockConfiguration GetVerifyMockConfiguration(VerifyMock mock);
		T GetReplaceMockConfiguration<T>(ReplaceMock mock);
		SourceMemberInsertMockConfiguration GetSourceMemberInsertMockConfiguration(SourceMemberInsertMock mock);
	}
}