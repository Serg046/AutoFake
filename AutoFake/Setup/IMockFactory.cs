using System;
using AutoFake.Expression;
using AutoFake.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake.Setup
{
	internal interface IMockFactory
	{
		InsertMock GetInsertMock(Action closure, InsertMock.Location location);
		T GetExpressionBasedMock<T>(IInvocationExpression expression) where T : IMock;
		SourceMemberInsertMock GetSourceMemberInsertMock(IInvocationExpression invocationExpression, Action closure, InsertMock.Location location);
		ReplaceInterfaceCallMock GetReplaceInterfaceCallMock(TypeReference typeReference);
		ReplaceValueTypeCtorMock GetReplaceValueTypeCtorMock(TypeReference typeReference);
		ReplaceReferenceTypeCtorMock GetReplaceReferenceTypeCtorMock(TypeReference typeReference);
		ReplaceTypeCastMock GetReplaceTypeCastMock(TypeReference typeReference);
	}
}