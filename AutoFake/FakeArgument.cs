namespace AutoFake
{
    internal class FakeArgument
    {
        private readonly IFakeArgumentChecker _checker;

        public FakeArgument(IFakeArgumentChecker checker)
        {
            _checker = checker;
        }

        public bool Check(object argument) => _checker.Check(argument);

        public override string ToString() => _checker.ToString();
    }
}
