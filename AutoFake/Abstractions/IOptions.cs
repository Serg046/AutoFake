using System;
using System.Collections.Generic;

namespace AutoFake.Abstractions;

public interface IOptions
{
	IList<Predicate<IMethodContract>> AllowedVirtualMembers { get; }
	bool DisableVirtualMembers { get; set; }
	DebugMode Debug { get; set; }
	AnalysisLevels AnalysisLevel { get; set; }
	IList<Type> ReferencedTypes { get; }
	void AddReference(Type type);
	bool IsMultipleAssembliesMode { get; }
	bool IsDebugEnabled { get; }
	string Key { get; }
}
