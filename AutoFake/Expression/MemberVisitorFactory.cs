using System;
using AutoFake.Abstractions.Expression;
using DryIoc;

namespace AutoFake.Expression
{
	internal class MemberVisitorFactory : IMemberVisitorFactory
	{
		private readonly IContainer _serviceLocator;

		public MemberVisitorFactory(IContainer serviceLocator) => _serviceLocator = serviceLocator;

		public T GetMemberVisitor<T>() => _serviceLocator.Resolve<T>();

		public IGetValueMemberVisitor GetValueMemberVisitor(object? instance)
			=> _serviceLocator.Resolve<Func<object?, IGetValueMemberVisitor>>().Invoke(instance);

		public ITargetMemberVisitor<T> GetTargetMemberVisitor<T>(IExecutableMemberVisitor<T> requestedVisitor, Type targetType)
			=> _serviceLocator.Resolve<Func<IExecutableMemberVisitor<T>, Type, ITargetMemberVisitor<T>>>().Invoke(requestedVisitor, targetType);
	}
}
