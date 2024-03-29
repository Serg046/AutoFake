using System;
using AutoFake.Abstractions;

namespace AutoFake;

internal class LambdaArgumentChecker : ILambdaArgumentChecker
{
	private readonly Delegate _checker;

	public LambdaArgumentChecker(Delegate checker)
	{
		_checker = checker;
	}

	public bool Check(object argument) => (bool)_checker.DynamicInvoke(argument)!;

	public override string ToString() => "should match Is-expression";
}
