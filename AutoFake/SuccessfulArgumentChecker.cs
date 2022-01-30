using AutoFake.Abstractions;

namespace AutoFake
{
    internal class SuccessfulArgumentChecker : IFakeArgumentChecker
    {
        public bool Check(object argument) => true;
    }
}
