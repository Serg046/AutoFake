using AutoFake.Setup.Mocks;
using System;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup.Configurations;

namespace AutoFake.Setup.Configurations
{
	internal class ReplaceMockConfiguration<TReturn> : IReplaceMockConfiguration<TReturn>
    {
        private readonly ReplaceMock _mock;

        internal ReplaceMockConfiguration(ReplaceMock mock)
        {
            _mock = mock;
        }

        public IReplaceMockConfiguration<TReturn> Return(TReturn returnObject)
        {
            _mock.ReturnObject = returnObject ?? throw new ArgumentNullException(nameof(returnObject));
            _mock.ReturnType = typeof(TReturn);
            return this;
        }

        public IReplaceMockConfiguration<TReturn> ExpectedCalls(uint expectedCallsCount)
        {
            return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
        }

        public IReplaceMockConfiguration<TReturn> ExpectedCalls(IExecutionContext.CallsCheckerFunc expectedCallsCountFunc)
        {
            _mock.SourceMemberMetaData.ExpectedCalls = expectedCallsCountFunc;
            return this;
        }

        public IReplaceMockConfiguration<TReturn> WhenArgumentsAreMatched()
        {
	        _mock.SourceMemberMetaData.InvocationExpression.ThrowWhenArgumentsAreNotMatched = false;
	        return this;
        }

        public IReplaceMockConfiguration<TReturn> When(IExecutionContext.WhenInstanceFunc when)
        {
            _mock.SourceMemberMetaData.WhenFunc = when;
            return this;
        }
    }
}
