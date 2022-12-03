using System;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup.Mocks;
using AutoFake.Abstractions.Setup.Mocks.ContractMocks;
using AutoFake.Setup.Mocks;
using Mono.Cecil;

namespace AutoFake.Abstractions.Setup;

public interface IMockFactory
{
	IInsertMock GetInsertMock(Action closure, IInsertMock.Location location);
	T GetExpressionBasedMock<T>(IInvocationExpression expression) where T : IMock;
	ISourceMemberInsertMock GetSourceMemberInsertMock(IInvocationExpression invocationExpression, Action closure, IInsertMock.Location location);
	IReplaceInterfaceCallMock GetReplaceInterfaceCallMock(TypeReference typeReference);
	IReplaceValueTypeCtorMock GetReplaceValueTypeCtorMock(TypeReference typeReference);
	IReplaceReferenceTypeCtorMock GetReplaceReferenceTypeCtorMock(TypeReference typeReference);
	IReplaceTypeCastMock GetReplaceTypeCastMock(TypeReference typeReference);
}
