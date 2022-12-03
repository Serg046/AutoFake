using System;

namespace AutoFake.Abstractions;

public interface IFakeObjectInfo
{
	object? Instance { get; }
	Type SourceType { get; }
}
