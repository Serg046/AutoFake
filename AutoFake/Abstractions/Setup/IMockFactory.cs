using System;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup.Mocks;
using AutoFake.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake.Abstractions.Setup
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
