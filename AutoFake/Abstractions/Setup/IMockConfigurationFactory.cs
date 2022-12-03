using System;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Mocks;

namespace AutoFake.Abstractions.Setup;

public interface IMockConfigurationFactory
{
	T GetInsertMockConfiguration<T>(IMockConfiguration mockConfiguration, Action<IMock> setMock, Action closure);
	IVerifyMockConfiguration GetVerifyMockConfiguration(IVerifyMock mock);
	T GetReplaceMockConfiguration<T>(IReplaceMock mock);
	ISourceMemberInsertMockConfiguration<T> GetSourceMemberInsertMockConfiguration<T>(ISourceMemberInsertMock mock);
}
