using System;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup.Mocks;

namespace AutoFake.Abstractions.Setup;

public interface IMockFactory
{
	IInsertMock GetInsertMock(Action closure, IInsertMock.Location location);
	T GetExpressionBasedMock<T>(IInvocationExpression expression) where T : IMock;
	ISourceMemberInsertMock GetSourceMemberInsertMock(IInvocationExpression invocationExpression, Action closure, IInsertMock.Location location);
}
