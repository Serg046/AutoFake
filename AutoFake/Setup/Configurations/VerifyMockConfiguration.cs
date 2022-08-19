using AutoFake.Setup.Mocks;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Configurations;

namespace AutoFake.Setup.Configurations
{
	internal class VerifyMockConfiguration : IVerifyMockConfiguration
	{
		private readonly VerifyMock _mock;

		internal VerifyMockConfiguration(VerifyMock mock)
		{
			_mock = mock;
			ExpectedCalls(callsCount => callsCount > 0);
		}

		public IVerifyMockConfiguration ExpectedCalls(uint expectedCallsCount)
		{
			return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
		}

		public IVerifyMockConfiguration ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc)
		{
			_mock.SourceMemberMetaData.ExpectedCalls = expectedCallsCountFunc;
			return this;
		}
	}
}
