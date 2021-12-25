using System;
using AutoFake.Expression;
using AutoFake.Setup.Mocks;

namespace AutoFake.Setup
{
	internal interface IMockFactory
	{
		InsertMock GetInsertMock(Action closure, InsertMock.Location location);
		T GetExpressionBasedMock<T>(IInvocationExpression expression) where T : IMock;
		SourceMemberInsertMock GetSourceMemberInsertMock(IInvocationExpression invocationExpression, Action closure, InsertMock.Location location);
	}
}