using System;

namespace AutoFake
{
    internal static class Guard
    {
        internal static void NotNull<T>(T value, string parameterName) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(parameterName);
        }
    }
}
