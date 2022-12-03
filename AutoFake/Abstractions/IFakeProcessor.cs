using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup;

namespace AutoFake.Abstractions;

public interface IFakeProcessor
{
	void ProcessMethod(IMockCollection mockCollection, IInvocationExpression invocationExpression, IFakeOptions options);
}
