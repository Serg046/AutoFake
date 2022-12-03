using AutoFake.Abstractions;

namespace AutoFake;

internal class GenericArgument : IGenericArgument
{
	public GenericArgument(string name, string type, string declaringType, string? genericDeclaringType = null)
	{
		Name = name;
		Type = type;
		DeclaringType = declaringType;
		GenericDeclaringType = genericDeclaringType;
	}

	public string Name { get; }
	public string Type { get; }
	public string DeclaringType { get; }
	public string? GenericDeclaringType { get; }
}
