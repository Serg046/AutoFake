namespace AutoFake.Abstractions
{
	public interface IMethodContract
	{
		string DeclaringType { get; }
		string ReturnType { get; }
		string Name { get; }
		string[] ParameterTypes { get; }
	}
}
