namespace AutoFake.Abstractions;

public interface IGenericArgument
{
	public delegate IGenericArgument Create(string name, string type, string declaringType, string? genericDeclaringType = null);
	string DeclaringType { get; }
	string? GenericDeclaringType { get; }
	string Name { get; }
	string Type { get; }
}
