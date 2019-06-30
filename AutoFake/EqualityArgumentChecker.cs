namespace AutoFake
{
    internal class EqualityArgumentChecker : IFakeArgumentChecker
    {
        private readonly object _value;

        public EqualityArgumentChecker(object value)
        {
            _value = value;
        }

        public bool Check(object argument) => _value.Equals(argument);

        public override string ToString() => _value.ToString();
    }
}
