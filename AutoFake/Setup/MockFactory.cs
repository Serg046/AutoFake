using System;
using AutoFake.Expression;
using AutoFake.Setup.Mocks;
using DryIoc;
using Mono.Cecil;

namespace AutoFake.Setup
{
	internal class MockFactory : IMockFactory
	{
		private readonly IContainer _serviceLocator;

		public MockFactory(IContainer serviceLocator) => _serviceLocator = serviceLocator;

		public InsertMock GetInsertMock(Action closure, InsertMock.Location location)
			=> _serviceLocator.Resolve<Func<Action, InsertMock.Location, InsertMock>>().Invoke(closure, location);

		public T GetExpressionBasedMock<T>(IInvocationExpression expression) where T: IMock
			=> _serviceLocator.Resolve<Func<IInvocationExpression, T>>().Invoke(expression);

		public SourceMemberInsertMock GetSourceMemberInsertMock(IInvocationExpression invocationExpression, Action closure, InsertMock.Location location)
			=> _serviceLocator.Resolve<Func<IInvocationExpression, Action, InsertMock.Location, SourceMemberInsertMock>>().Invoke(invocationExpression, closure, location);

		public ReplaceInterfaceCallMock GetReplaceInterfaceCallMock(TypeReference typeReference)
			=> _serviceLocator.Resolve<Func<TypeReference, ReplaceInterfaceCallMock>>().Invoke(typeReference);

		public ReplaceValueTypeCtorMock GetReplaceValueTypeCtorMock(TypeReference typeReference)
			=> _serviceLocator.Resolve<Func<TypeReference, ReplaceValueTypeCtorMock>>().Invoke(typeReference);

		public ReplaceReferenceTypeCtorMock GetReplaceReferenceTypeCtorMock(TypeReference typeReference)
			=> _serviceLocator.Resolve<Func<TypeReference, ReplaceReferenceTypeCtorMock>>().Invoke(typeReference);

		public ReplaceTypeCastMock GetReplaceTypeCastMock(TypeReference typeReference)
			=> _serviceLocator.Resolve<Func<TypeReference, ReplaceTypeCastMock>>().Invoke(typeReference);
	}
}
