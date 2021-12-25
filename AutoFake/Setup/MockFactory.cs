using System;
using AutoFake.Expression;
using AutoFake.Setup.Mocks;
using DryIoc;

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
	}
}
