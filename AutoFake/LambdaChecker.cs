namespace AutoFake
{
    internal class LambdaChecker : IFakeArgumentChecker
    {
        private readonly dynamic _checker;

        public LambdaChecker(dynamic checker)
        {
            _checker = checker;
        }

        public bool Check(dynamic argument) => _checker(argument);
    }
}
