using System;
using System.Collections.Generic;
using System.Reflection;

namespace AutoFake
{
    public class FakeOptions
    {
        public List<Predicate<MethodContract>> AllowedVirtualMembers { get; } = new ();
        public bool DisableVirtualMembers { get; set; }
        public DebugMode Debug { get; set; } = DebugMode.Auto;
        public AnalysisLevels AnalysisLevel { get; set; } = AnalysisLevels.Assembly;
        public IList<Assembly> Assemblies { get; } = new List<Assembly>();
    }
}
