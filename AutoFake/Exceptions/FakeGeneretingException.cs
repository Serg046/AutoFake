using System;

namespace AutoFake.Exceptions
{
    public class FakeGeneretingException : Exception
    {
        public FakeGeneretingException(string message) : base(message)
        {
        }
    }
}
