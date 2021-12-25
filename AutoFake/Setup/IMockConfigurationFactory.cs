using System;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;

namespace AutoFake.Setup
{
	internal interface IMockConfigurationFactory
	{
		T GetAppendMockConfiguration<T>(Action<IMock> setMock, Action closure) where T : AppendMockConfiguration;
		T GetPrependMockConfiguration<T>(Action<IMock> setMock, Action closure) where T : PrependMockConfiguration;
		VerifyMockConfiguration GetVerifyMockConfiguration(VerifyMock mock);
		T GetReplaceMockConfiguration<T>(ReplaceMock mock);
		SourceMemberInsertMockConfiguration GetSourceMemberInsertMockConfiguration(SourceMemberInsertMock mock);
	}
}