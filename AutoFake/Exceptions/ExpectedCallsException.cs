using System;

namespace AutoFake.Exceptions
{
    public class ExpectedCallsException : Exception
    {
        public ExpectedCallsException(string message) : base(message)
        {
        }
    }
}
