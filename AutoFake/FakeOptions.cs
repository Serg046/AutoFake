using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace AutoFake
{
    public class FakeOptions
    {
        public List<Predicate<MethodContract>> AllowedVirtualMembers { get; } = new ();
        public bool DisableVirtualMembers { get; set; }
        public bool Debug { get; set; } = Debugger.IsAttached;
        public AnalysisLevels AnalysisLevel { get; set; } = AnalysisLevels.Assembly;
        public IList<Assembly> Assemblies { get; } = new List<Assembly>();
    }
}
