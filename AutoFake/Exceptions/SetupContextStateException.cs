using System;

namespace AutoFake.Exceptions
{
    public class SetupContextStateException : Exception
    {
        public SetupContextStateException(string message) : base(message)
        {
        }
    }
}
