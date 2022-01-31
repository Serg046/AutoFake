using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Setup.Mocks;

namespace AutoFake.Setup.Configurations
{
	internal class SourceMemberInsertMockConfiguration : ISourceMemberInsertMockConfiguration
    {
        private readonly SourceMemberInsertMock _mock;

        internal SourceMemberInsertMockConfiguration(SourceMemberInsertMock mock)
        {
            _mock = mock;
        }

        public ISourceMemberInsertMockConfiguration ExpectedCalls(uint expectedCallsCount)
        {
            return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
        }

        public ISourceMemberInsertMockConfiguration ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc)
        {
            _mock.SourceMemberMetaData.ExpectedCalls = expectedCallsCountFunc;
            return this;
        }

        public ISourceMemberInsertMockConfiguration WhenArgumentsAreMatched()
        {
	        _mock.SourceMemberMetaData.InvocationExpression.ThrowWhenArgumentsAreNotMatched = false;
	        return this;
        }
    }
}
