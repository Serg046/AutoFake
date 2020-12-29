using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace AutoFake
{
    public class FakeOptions
    {
        public IList<string> VirtualMembers { get; } = new List<string>();
        public bool IncludeAllVirtualMembers { get; set; }
        public bool Debug { get; set; } = Debugger.IsAttached;
        public AnalysisLevels AnalysisLevel { get; set; } = AnalysisLevels.Assembly;
        public IList<Assembly> Assemblies { get; } = new List<Assembly>();
    }
}
