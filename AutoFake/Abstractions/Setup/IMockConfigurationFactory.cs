﻿using System;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Mocks;
using AutoFake.Setup.Mocks;

namespace AutoFake.Abstractions.Setup
{
	internal interface IMockConfigurationFactory
	{
		T GetInsertMockConfiguration<T>(Action<IMock> setMock, Action closure);
		IVerifyMockConfiguration GetVerifyMockConfiguration(VerifyMock mock);
		T GetReplaceMockConfiguration<T>(ReplaceMock mock);
		ISourceMemberInsertMockConfiguration GetSourceMemberInsertMockConfiguration(SourceMemberInsertMock mock);
	}
}