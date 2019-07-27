using System;

namespace AutoFake.Exceptions
{
    public class VerifyException : Exception
    {
        public VerifyException(string message) : base(message)
        {
        }
    }
}
