using AutoFake.Abstractions;

namespace AutoFake;

internal class SuccessfulArgumentChecker : ISuccessfulArgumentChecker
{
	public bool Check(object argument) => true;
}
