using AutoFake.Abstractions;

namespace AutoFake;

internal class MethodContract : IMethodContract
{
	public MethodContract(string declaringType, string returnType, string name, string[] parameterTypes)
	{
		DeclaringType = declaringType;
		ReturnType = returnType;
		Name = name;
		ParameterTypes = parameterTypes;
	}

	public string DeclaringType { get; }
	public string ReturnType { get; }
	public string Name { get; }
	public string[] ParameterTypes { get; }
}
