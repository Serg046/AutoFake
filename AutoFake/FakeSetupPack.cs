using System.Reflection;

namespace AutoFake
{
    internal class FakeSetupPack
    {
        public MethodInfo Method { get; internal set; }
        public object ReturnObject { get; internal set; }
        public bool IsVerifiable { get; internal set; }
        public object[] SetupArguments { get; internal set; }
        public int ExpectedCallsCount { get; internal set; }
        public bool IsVoid { get; internal set; }
    }
}
