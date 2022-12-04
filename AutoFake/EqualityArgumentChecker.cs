using AutoFake.Abstractions;

namespace AutoFake;

internal class EqualityArgumentChecker : IEqualityArgumentChecker
{
	private readonly object _value;
	private readonly IFakeArgumentChecker.Comparer _comparer;

	public EqualityArgumentChecker(object value, IFakeArgumentChecker.Comparer? comparer = null)
	{
		_value = value;
		_comparer = comparer ?? Equals;
	}

	public bool Check(object argument) => _comparer(_value, argument);

	public override string ToString() => ToString(_value);

	public static string ToString(object value) => value is string str ? $"\"{str}\"" : value?.ToString() ?? "null";
}
