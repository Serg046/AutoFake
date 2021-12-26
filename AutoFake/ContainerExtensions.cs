using System;
using AutoFake.Expression;
using AutoFake.Setup;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using DryIoc;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake
{
	internal static class ContainerExtensions
	{
		public static Container CreateContainer(Type sourceType, Fake fake)
		{
			var container = new Container(rules => rules.WithFuncAndLazyWithoutRegistration());
			container.RegisterInstance(fake);
			var fakeOptions = new FakeOptions();
			container.RegisterInstance(fakeOptions);
			container.Register<IAssemblyReader, AssemblyReader>(Reuse.Singleton,
				Parameters.Of.Type<Type>(defaultValue: sourceType)
					.OverrideWith(Parameters.Of.Type<FakeOptions>(defaultValue: fakeOptions)));

			container.Register<MockCollection>();
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
			container.RegisterDelegate<InvocationExpression.Create>(ctx =>
				expr => new InvocationExpression(ctx.Resolve<IMemberVisitorFactory>(), expr));

			//todo: shouldn't be a singleton, there is an issue with scopes 
			container.Register<IMockConfigurationFactory, MockConfigurationFactory>(Reuse.Singleton);
			container.Register<IMockFactory, MockFactory>(Reuse.Singleton);
			container.Register<IMemberVisitorFactory, MemberVisitorFactory>(Reuse.Singleton);

			AddConfigurations(container);
			AddMocks(container);
			AddMemberVisitors(container);
			return container;
		}

		private static void AddConfigurations(IRegistrator container)
		{
			container.Register<AppendMockConfiguration>(made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register(typeof(AppendMockConfiguration<>), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register<PrependMockConfiguration>(made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register(typeof(PrependMockConfiguration<>), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register(typeof(VerifyMockConfiguration), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register(typeof(ReplaceMockConfiguration<>), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register(typeof(RemoveMockConfiguration), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register(typeof(SourceMemberInsertMockConfiguration), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
		}

		private static void AddMocks(IRegistrator container)
		{
			container.Register<InsertMock>();
			container.Register<VerifyMock>();
			container.Register<ReplaceMock>();
			container.Register<SourceMemberInsertMock>();
		}

		private static void AddMemberVisitors(IRegistrator container)
		{
			container.Register<GetArgumentsMemberVisitor>();
			container.Register<GetSourceMemberVisitor>();
			container.Register<GetTestMethodVisitor>();
			container.Register<GetValueMemberVisitor>();
			container.Register<TargetMemberVisitor>();
		}

		public static IResolverContext AddInvocationExpression(this Container container, LinqExpression expression, bool addMocks = false)
		{
            var invocationExpression = new InvocationExpression(container.Resolve<IMemberVisitorFactory>(), expression ?? throw new ArgumentNullException(nameof(expression)));
            var scope = container.OpenScope(invocationExpression);
			scope.Use<IInvocationExpression>(_ => invocationExpression);

			if (addMocks)
			{
				var mocks = new MockCollection();
				container.Use<IMockCollection>(_ => mocks);
				container.RegisterInstance<IMockCollection>(mocks, serviceKey: invocationExpression);
			}

			return scope;
		}
	}
}
