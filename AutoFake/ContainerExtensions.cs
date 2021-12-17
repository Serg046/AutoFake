using System;
using AutoFake.Expression;
using AutoFake.Setup;
using AutoFake.Setup.Configurations;
using DryIoc;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake
{
	internal static class ContainerExtensions
	{
		public static void AddServices(this Container container, Type sourceType, Fake fake, FakeOptions fakeOptions)
		{
			container.Register<MockCollection>();
			container.RegisterInstance(fakeOptions);
			container.Register<IAssemblyReader, AssemblyReader>(Reuse.Singleton,
				Parameters.Of.Type<Type>(defaultValue: sourceType)
					.OverrideWith(Parameters.Of.Type<FakeOptions>(defaultValue: fakeOptions)));
			container.Register<ITypeInfo, TypeInfo>(Reuse.Singleton);
			container.Register<IAssemblyWriter, AssemblyWriter>(Reuse.Singleton);
			container.Register<IAssemblyHost, AssemblyHost>(Reuse.Singleton);
			container.Register<IAssemblyPool, AssemblyPool>(Reuse.Singleton);
			container.Register<IFakeProcessor, FakeProcessor>();
			container.Register<IProcessorFactory, ProcessorFactory>();
			container.Register(typeof(ActionMockConfiguration<>), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register(typeof(ActionMockConfiguration), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register(typeof(FuncMockConfiguration<,>), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register(typeof(FuncMockConfiguration<>), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register<Executor>();
			container.Register(typeof(Executor<>));
			container.RegisterInstance(fake);
		}

		public static IResolverContext AddInvocationExpression(this Container container, LinqExpression expression)
		{
            var invocationExpression = new InvocationExpression(expression ?? throw new ArgumentNullException(nameof(expression)));
            var scope = container.OpenScope(invocationExpression);
			scope.Use<IInvocationExpression>(_ => invocationExpression);
            container.RegisterDelegate<IInvocationExpression>(() => invocationExpression, Reuse.ScopedTo(invocationExpression));

			var mocks = new MockCollection();
			container.RegisterDelegate<IMockCollection>(() => mocks, Reuse.ScopedTo(invocationExpression));
			container.RegisterInstance<IMockCollection>(mocks, serviceKey: invocationExpression);
			return scope;
		}
	}
}
