using System;
using AutoFake.Setup;

namespace AutoFake
{
    public static class Arg
    {
        public static FakeDependency IsNull<T>()
        {
            var type = typeof(T);
            if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
                throw new NotSupportedException("Value type instance cannot be null");

            return new FakeDependency(type, null);
        }

        public static T Is<T>(Func<T, bool> checkArgumentFunc)
        {
            SetupContext.SetCurrentChecker(new Checker(checkArgumentFunc));
            return default(T);
        }

        private class Checker : IFakeArgumentChecker
        {
            private readonly dynamic _checker;

            public Checker(dynamic checker)
            {
                _checker = checker;
            }

            public bool Check(dynamic argument) => _checker(argument);
        }
    }
}
