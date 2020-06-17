using System;

namespace AutoFake
{
    internal class LambdaArgumentChecker : IFakeArgumentChecker
    {
        private readonly Delegate _checker;

        public LambdaArgumentChecker(Delegate checker)
        {
            _checker = checker;
        }

        public bool Check(object argument) => (bool)_checker.DynamicInvoke(argument);

        public override string ToString() => "should match Is-expression";
    }
}
