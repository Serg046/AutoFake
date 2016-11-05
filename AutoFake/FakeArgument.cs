using System;
using AutoFake.Setup;

namespace AutoFake
{
    public class FakeArgument
    {
        private readonly IFakeArgumentChecker _checker;

        internal FakeArgument(IFakeArgumentChecker checker)
        {
            _checker = checker;
        }

        public static T Satisfies<T>(Func<T, bool> checkArgumentFunc)
        {
            SetupContext.SetCurrentChecker(new Checker(checkArgumentFunc));
            return default(T);
        }

        internal bool Check(dynamic argument) => _checker.Check(argument);

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
