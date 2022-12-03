namespace AutoFake.Abstractions;

public interface IFakeArgument
{
	IFakeArgumentChecker Checker { get; }
	bool Check(object argument);
}
