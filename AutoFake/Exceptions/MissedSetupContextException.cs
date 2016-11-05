using System;

namespace AutoFake.Exceptions
{
    class MissedSetupContextException : Exception
    {
        public MissedSetupContextException(string message) : base(message)
        {
        }
    }
}
