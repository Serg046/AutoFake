namespace AutoFake
{
    internal class FakeArgument
    {
        private readonly IFakeArgumentChecker _checker;

        public FakeArgument(IFakeArgumentChecker checker)
        {
            _checker = checker;
        }

        public bool Check(dynamic argument) => _checker.Check(argument);
    }
}
