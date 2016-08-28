using System;

namespace AutoFake.Exceptions
{
    public class NotSupportedExpressionException : Exception
    {
        public NotSupportedExpressionException(string message) : base(message)
        {
        }
    }
}
