using AutoFake.Abstractions;

namespace AutoFake;

internal class FakeArgument : IFakeArgument
{
	public FakeArgument(IFakeArgumentChecker checker)
	{
		Checker = checker;
	}

	public IFakeArgumentChecker Checker { get; }

	public bool Check(object argument) => Checker.Check(argument);

	public override string? ToString() => Checker.ToString();
}
