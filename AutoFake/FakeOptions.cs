﻿using System.Collections.Generic;

namespace AutoFake
{
    public class FakeOptions
    {
        public IList<string> VirtualMembers { get; } = new List<string>();
        public bool IncludeAllVirtualMembers { get; set; }
    }
}