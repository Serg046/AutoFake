using System;
using AutoFake.Abstractions.Expression;
using DryIoc;

namespace AutoFake.Expression
{
	internal class MemberVisitorFactory : IMemberVisitorFactory
	{
		private readonly IContainer _serviceLocator;

		public MemberVisitorFactory(IContainer serviceLocator) => _serviceLocator = serviceLocator;

		public T GetMemberVisitor<T>() where T : IMemberVisitor => _serviceLocator.Resolve<T>();

		public GetValueMemberVisitor GetValueMemberVisitor(object? instance)
			=> _serviceLocator.Resolve<Func<object?, GetValueMemberVisitor>>().Invoke(instance);

		public TargetMemberVisitor GetTargetMemberVisitor(IMemberVisitor requestedVisitor, Type targetType)
			=> _serviceLocator.Resolve<Func<IMemberVisitor, Type, TargetMemberVisitor>>().Invoke(requestedVisitor, targetType);
	}
}
