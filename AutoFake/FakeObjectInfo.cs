using System;

namespace AutoFake
{
	internal class FakeObjectInfo
	{
		public delegate FakeObjectInfo Create(Type sourceType, object? instance);
		public FakeObjectInfo(Type sourceType, object? instance)
		{
			SourceType = sourceType;
			Instance = instance;
		}

		public Type SourceType { get; }
		public object? Instance { get; }
	}
}
