using System;

namespace AutoFake.Exceptions
{
    public class SetupException : Exception
    {
        public SetupException(string message) : base(message)
        {
        }
    }
}
