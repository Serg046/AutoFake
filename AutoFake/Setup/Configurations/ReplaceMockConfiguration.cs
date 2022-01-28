using AutoFake.Setup.Mocks;
using System;

namespace AutoFake.Setup.Configurations
{
    public class ReplaceMockConfiguration<TReturn>
    {
        private readonly ReplaceMock _mock;

        internal ReplaceMockConfiguration(ReplaceMock mock)
        {
            _mock = mock;
        }

        public ReplaceMockConfiguration<TReturn> Return(TReturn returnObject)
        {
            _mock.ReturnObject = returnObject ?? throw new ArgumentNullException(nameof(returnObject));
            _mock.ReturnType = typeof(TReturn);
            return this;
        }

        public ReplaceMockConfiguration<TReturn> ExpectedCalls(uint expectedCallsCount)
        {
            return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
        }

        public ReplaceMockConfiguration<TReturn> ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc)
        {
            _mock.SourceMemberMetaData.ExpectedCalls = expectedCallsCountFunc;
            return this;
        }

        public ReplaceMockConfiguration<TReturn> WhenArgumentsAreMatched()
        {
	        _mock.SourceMemberMetaData.InvocationExpression.ThrowWhenArgumentsAreNotMatched = false;
	        return this;
        }
    }
}
