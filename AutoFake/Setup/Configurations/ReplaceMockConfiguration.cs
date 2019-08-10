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

        public ReplaceMockConfiguration<TReturn> Return(Func<TReturn> returnObject)
        {
            _mock.ReturnObject = new MethodDescriptor(returnObject.Method.DeclaringType.FullName, returnObject.Method.Name);
            return this;
        }

        public ReplaceMockConfiguration<TReturn> CheckArguments()
        {
            _mock.CheckArguments = true;
            return this;
        }

        //public ReplaceMockInstaller<TReturn> ExpectedCalls(byte expectedCallsCount)
        //{
        //    return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
        //}

        public ReplaceMockConfiguration<TReturn> ExpectedCalls(Func<byte, bool> expectedCallsCountFunc)
        {
            _mock.ExpectedCalls = expectedCallsCountFunc.ToMethodDescriptor();
            return this;
        }
    }
}
