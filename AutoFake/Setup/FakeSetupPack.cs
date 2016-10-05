using System;
using System.Reflection;

namespace AutoFake.Setup
{
    internal class FakeSetupPack
    {
        public MethodInfo Method { get; internal set; }
        public object ReturnObject { get; internal set; }
        public string ReturnObjectFieldName { get; internal set; }
        public object[] SetupArguments { get; internal set; }
        public bool NeedCheckArguments { get; internal set; }
        public bool NeedCheckCallsCount { get; internal set; }
        public Func<int, bool> ExpectedCallsCountFunc { get; internal set; }
        public bool IsVoid { get; internal set; }
        public bool IsVerification { get; internal set; }
        internal bool IsReturnObjectSet { get; set; }
    }
}
