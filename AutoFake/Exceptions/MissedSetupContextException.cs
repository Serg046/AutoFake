using System;

namespace AutoFake.Exceptions
{
    public class MissedSetupContextException : Exception
    {
        public MissedSetupContextException(string message) : base(message)
        {
        }
    }
}
