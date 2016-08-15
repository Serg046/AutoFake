using System;
using System.Collections.Generic;

namespace AutoFake
{
    internal class GeneratedObject
    {
        public object Instance { get; internal set; }
        public Type Type { get; internal set; }
        public IList<MockedMemberInfo> MockedMembers { get; internal set; } 
    }
}
