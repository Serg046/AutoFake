using System;

namespace AutoFake.Exceptions
{
    public class VerifiableException : Exception
    {
        public VerifiableException(string message) : base(message)
        {
        }
    }
}
