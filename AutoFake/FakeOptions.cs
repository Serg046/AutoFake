using System;
using System.Collections.Generic;
using AutoFake.Abstractions;

namespace AutoFake
{
    internal class FakeOptions : IFakeOptions
    {
        public IList<Predicate<IMethodContract>> AllowedVirtualMembers { get; } = new List<Predicate<IMethodContract>>();
        public bool DisableVirtualMembers { get; set; }
        public DebugMode Debug { get; set; } = DebugMode.Auto;
        public AnalysisLevels AnalysisLevel { get; set; } = AnalysisLevels.Assembly;
        //TODO: Move it from here
        public IList<Type> ReferencedTypes { get; } = new List<Type>();
        public void AddReference(Type type) => ReferencedTypes.Add(type);
        public bool IsMultipleAssembliesMode => AnalysisLevel == AnalysisLevels.AllExceptSystemAndMicrosoft || ReferencedTypes.Count > 0;
    }
}
