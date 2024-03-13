using System;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Mocks;
using DryIoc;
using Mono.Cecil;

namespace AutoFake.Setup;

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
}
