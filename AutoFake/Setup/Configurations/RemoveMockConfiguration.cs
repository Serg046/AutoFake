using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Setup.Mocks;

namespace AutoFake.Setup.Configurations
{
	internal class RemoveMockConfiguration : IRemoveMockConfiguration
    {
        private readonly ReplaceMock _mock;

        internal RemoveMockConfiguration(ReplaceMock mock)
        {
            _mock = mock;
        }

        public IRemoveMockConfiguration ExpectedCalls(uint expectedCallsCount)
        {
            return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
        }

        public IRemoveMockConfiguration ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc)
        {
            _mock.SourceMemberMetaData.ExpectedCalls = expectedCallsCountFunc;
            return this;
        }

        public IRemoveMockConfiguration WhenArgumentsAreMatched()
        {
	        _mock.SourceMemberMetaData.InvocationExpression.ThrowWhenArgumentsAreNotMatched = false;
	        return this;
        }
    }
}
