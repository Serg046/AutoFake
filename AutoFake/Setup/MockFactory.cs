using System;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Mocks;
using AutoFake.Abstractions.Setup.Mocks.ContractMocks;
using AutoFake.Setup.Mocks;
using DryIoc;
using Mono.Cecil;

namespace AutoFake.Setup
{
	internal class MockFactory : IMockFactory
	{
		private readonly IContainer _serviceLocator;

		public MockFactory(IContainer serviceLocator) => _serviceLocator = serviceLocator;

		public IInsertMock GetInsertMock(Action closure, IInsertMock.Location location)
			=> _serviceLocator.Resolve<Func<Action, IInsertMock.Location, IInsertMock>>().Invoke(closure, location);

		public T GetExpressionBasedMock<T>(IInvocationExpression expression) where T : IMock
			=> _serviceLocator.Resolve<Func<IInvocationExpression, T>>().Invoke(expression);

		public ISourceMemberInsertMock GetSourceMemberInsertMock(IInvocationExpression invocationExpression, Action closure, IInsertMock.Location location)
			=> _serviceLocator.Resolve<Func<IInvocationExpression, Action, IInsertMock.Location, ISourceMemberInsertMock>>().Invoke(invocationExpression, closure, location);

		public IReplaceInterfaceCallMock GetReplaceInterfaceCallMock(TypeReference typeReference)
			=> _serviceLocator.Resolve<Func<TypeReference, IReplaceInterfaceCallMock>>().Invoke(typeReference);

		public IReplaceValueTypeCtorMock GetReplaceValueTypeCtorMock(TypeReference typeReference)
			=> _serviceLocator.Resolve<Func<TypeReference, IReplaceValueTypeCtorMock>>().Invoke(typeReference);

		public IReplaceReferenceTypeCtorMock GetReplaceReferenceTypeCtorMock(TypeReference typeReference)
			=> _serviceLocator.Resolve<Func<TypeReference, IReplaceReferenceTypeCtorMock>>().Invoke(typeReference);

		public IReplaceTypeCastMock GetReplaceTypeCastMock(TypeReference typeReference)
			=> _serviceLocator.Resolve<Func<TypeReference, IReplaceTypeCastMock>>().Invoke(typeReference);
	}
}
