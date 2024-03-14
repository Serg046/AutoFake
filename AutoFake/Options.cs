using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutoFake.Abstractions;

namespace AutoFake;

internal class Options : IOptions
{
	public Options(string key)
	{
		Key = key;
	}

	public bool IsDebugEnabled => Debug == DebugMode.Enabled || (Debug == DebugMode.Auto && Debugger.IsAttached);
	public bool IsMultipleAssembliesMode => AnalysisLevel == AnalysisLevels.AllExceptSystemAndMicrosoft || ReferencedTypes.Count > 0;
	public IList<Predicate<IMethodContract>> AllowedVirtualMembers { get; } = new List<Predicate<IMethodContract>>();
	public bool DisableVirtualMembers { get; set; }
	public DebugMode Debug { get; set; } = DebugMode.Auto;
	public AnalysisLevels AnalysisLevel { get; set; } = AnalysisLevels.Assembly;
	public IList<Type> ReferencedTypes { get; } = new List<Type>();
	public void AddReference(Type type) => ReferencedTypes.Add(type);
	public string Key { get; }
}
