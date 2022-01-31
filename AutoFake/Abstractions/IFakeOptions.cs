using System;
using System.Collections.Generic;

namespace AutoFake.Abstractions
{
	public interface IFakeOptions
	{
		IList<Predicate<IMethodContract>> AllowedVirtualMembers { get; }
		bool DisableVirtualMembers { get; set; }
		DebugMode Debug { get; set; }
		AnalysisLevels AnalysisLevel { get; set; }
		bool IsMultipleAssembliesMode { get; }
        IList<Type> ReferencedTypes { get; }
		void AddReference(Type type);
	}
}