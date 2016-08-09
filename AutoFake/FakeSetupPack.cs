﻿using System.Collections.Generic;
using System.Reflection;

namespace AutoFake
{
    internal class FakeSetupPack
    {
        public MethodInfo Method { get; internal set; }
        public object ReturnObject { get; internal set; }
        public IEnumerable<MethodInfo> ReachableWithCollection { get; internal set; }
        public bool IsVerifiable { get; internal set; }
        public object[] SetupArguments { get; internal set; }
    }
}
