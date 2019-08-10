﻿using AutoFake.Setup.Mocks;
using System;

namespace AutoFake.Setup.Configurations
{
    public class RemoveMockConfiguration
    {
        private readonly ReplaceMock _mock;

        internal RemoveMockConfiguration(ReplaceMock mock)
        {
            _mock = mock;
        }

        public RemoveMockConfiguration CheckArguments()
        {
            _mock.CheckArguments = true;
            return this;
        }

        //public RemoveMockInstaller ExpectedCalls(byte expectedCallsCount)
        //{
        //    return ExpectedCalls(callsCount => callsCount == expectedCallsCount);
        //}

        public RemoveMockConfiguration ExpectedCalls(Func<byte, bool> expectedCallsCountFunc)
        {
            _mock.ExpectedCalls = expectedCallsCountFunc.ToMethodDescriptor();
            return this;
        }
    }
}