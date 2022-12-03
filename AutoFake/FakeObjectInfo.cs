using System;
using AutoFake.Abstractions;

namespace AutoFake;

internal class FakeObjectInfo : IFakeObjectInfo
{
	public delegate IFakeObjectInfo Create(Type sourceType, object? instance);
	public FakeObjectInfo(Type sourceType, object? instance)
	{
		SourceType = sourceType;
		Instance = instance;
	}

	public Type SourceType { get; }
	public object? Instance { get; }
}
