using System;

namespace AutoFake
{
    internal class EqualityArgumentChecker : IFakeArgumentChecker
    {
        private readonly Func<dynamic, bool> _checker;

        public EqualityArgumentChecker(dynamic value)
        {
            _checker = dynValue => object.Equals(dynValue, value);
        }

        public bool Check(dynamic argument) => _checker.Invoke(argument);
    }
}
