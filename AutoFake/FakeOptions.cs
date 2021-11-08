using System;
using System.Collections.Generic;

namespace AutoFake
{
    public class FakeOptions
    {
        public List<Predicate<MethodContract>> AllowedVirtualMembers { get; } = new ();
        public bool DisableVirtualMembers { get; set; }
        public DebugMode Debug { get; set; } = DebugMode.Auto;
        public AnalysisLevels AnalysisLevel { get; set; } = AnalysisLevels.Assembly;
        internal IList<Type> ReferencedTypes { get; } = new List<Type>();
        public void AddReference(Type type) => ReferencedTypes.Add(type);

        public bool IsMultipleAssembliesMode => AnalysisLevel == AnalysisLevels.AllExceptSystemAndMicrosoft || ReferencedTypes.Count > 0;
    }
}
