using System;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Expression;
using AutoFake.Abstractions.Setup;
using AutoFake.Abstractions.Setup.Configurations;
using AutoFake.Abstractions.Setup.Mocks;
using AutoFake.Abstractions.Setup.Mocks.ContractMocks;
using AutoFake.Expression;
using AutoFake.Setup;
using AutoFake.Setup.Configurations;
using AutoFake.Setup.Mocks;
using AutoFake.Setup.Mocks.ContractMocks;
using DryIoc;
using Mono.Cecil;
using Mono.Cecil.Cil;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake
{
	internal static class ContainerExtensions
	{
		public static Container CreateContainer(Type sourceType, Action<Container> fakeRegistration)
		{
			var container = new Container(rules => rules.WithFuncAndLazyWithoutRegistration());
			fakeRegistration(container);
			var fakeOptions = new FakeOptions();
			container.RegisterInstance<IFakeOptions>(fakeOptions);
			container.Register<IAssemblyReader, AssemblyReader>(Reuse.Singleton,
				Parameters.Of.Type<Type>(defaultValue: sourceType)
					.OverrideWith(Parameters.Of.Type<IFakeOptions>(defaultValue: fakeOptions)));

			container.Register<MockCollection>();
			container.Register<ITypeInfo, TypeInfo>(Reuse.Singleton);
			container.Register<IAssemblyWriter, AssemblyWriter>(Reuse.Singleton);
			container.Register<IAssemblyLoader, AssemblyLoader>(Reuse.Singleton);
			container.Register<IAssemblyHost, AssemblyHost>(Reuse.Singleton);
			container.Register<IAssemblyPool, AssemblyPool>(Reuse.Singleton);
			container.Register<IFakeProcessor, FakeProcessor>();
			container.Register<ExpressionExecutorImpl>();
			container.Register<ExpressionExecutor>();
			container.Register(typeof(ExpressionExecutor<>));
			container.RegisterInstance<IExecutionContext.Create>((callsChecker, whenFunc) => new ExecutionContext(callsChecker, whenFunc));
			container.RegisterDelegate<InvocationExpression.Create>(ctx =>
				expr => new InvocationExpression(ctx.Resolve<IMemberVisitorFactory>(), expr));

			//todo: shouldn't be a singleton, there is an issue with scopes 
			container.Register<IMockConfigurationFactory, MockConfigurationFactory>(Reuse.Singleton);
			container.Register<IMockFactory, MockFactory>(Reuse.Singleton);
			container.Register<IMemberVisitorFactory, MemberVisitorFactory>(Reuse.Singleton);

			container.Register<LambdaArgumentChecker>();
			container.Register<FakeArgument>();
			container.Register<SuccessfulArgumentChecker>();
			container.Register<EqualityArgumentChecker>();
			container.Register<SourceMethod>(made: FactoryMethod.ConstructorWithResolvableArguments);
			container.Register<SourceField>();
			container.Register<ITypeMap, TypeMap>();
			container.RegisterInstance<FakeObjectInfo.Create>((srcType, instance) => new FakeObjectInfo(srcType, instance));
			container.Register<IContractProcessor, ContractProcessor>();
			container.Register<Emitter>();
			container.Register<IEmitterPool, EmitterPool>(setup: DryIoc.Setup.With(allowDisposableTransient: true));
			container.Register<TestMethod>();
			container.Register<IPrePostProcessor, PrePostProcessor>();
			container.Register<IProcessor, Processor>();
			container.Register<IGenericArgumentProcessor, GenericArgumentProcessor>();
			container.RegisterInstance<GenericArgument.Create>((name, type, declaringType, genericDeclaringType) =>
				new GenericArgument(name, type, declaringType, genericDeclaringType));
			container.Register<IMethodContract, MethodContract>();

			AddConfigurations(container);
			AddMocks(container);
			AddMemberVisitors(container);
			AddCecilFactory(container);
			return container;
		}

		private static void AddConfigurations(IRegistrator container)
		{
			container.Register<IMockConfiguration, MockConfiguration>();
			container.Register(typeof(IActionMockConfiguration<>), typeof(ActionMockConfiguration<>), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register(typeof(IFuncMockConfiguration<,>), typeof(FuncMockConfiguration<,>), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register(typeof(IAppendMockConfiguration<>), typeof(AppendMockConfiguration<>), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register(typeof(IPrependMockConfiguration<>), typeof(PrependMockConfiguration<>), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register<IVerifyMockConfiguration, VerifyMockConfiguration>(made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register(typeof(IReplaceMockConfiguration<,>), typeof(ReplaceMockConfiguration<,>), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register(typeof(IRemoveMockConfiguration<>), typeof(RemoveMockConfiguration<>), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register(typeof(ISourceMemberInsertMockConfiguration<>), typeof(SourceMemberInsertMockConfiguration<>), made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
		}

		private static void AddMocks(IRegistrator container)
		{
			container.Register<ISourceMemberMetaData, SourceMemberMetaData>(made: Made.Of(FactoryMethod.Constructor(includeNonPublic: true)));
			container.Register<ISourceMemberInsertMockInjector, SourceMemberInsertMockInjector>();
			container.Register<InsertMock>();
			container.Register<VerifyMock>();
			container.Register<ReplaceMock>();
			container.Register<SourceMemberInsertMock>();
			container.Register<IReplaceInterfaceCallMock, ReplaceInterfaceCallMock>();
			container.Register<IReplaceValueTypeCtorMock, ReplaceValueTypeCtorMock>();
			container.Register<IReplaceReferenceTypeCtorMock, ReplaceReferenceTypeCtorMock>();
			container.Register<IReplaceTypeCastMock, ReplaceTypeCastMock>();
		}

		private static void AddMemberVisitors(IRegistrator container)
		{
			container.Register<GetArgumentsMemberVisitor>();
			container.Register<IGetSourceMemberVisitor, GetSourceMemberVisitor>();
			container.Register<GetTestMethodVisitor>();
			container.Register<GetValueMemberVisitor>();
			container.Register(typeof(TargetMemberVisitor<>));
		}

		private static void AddCecilFactory(IRegistrator container)
		{
			container.Register<ICecilFactory, CecilFactory>();
			container.Register<VariableDefinition>();
			container.Register<ReaderParameters>(made: Made.Of(FactoryMethod.DefaultConstructor()));
			container.Register<WriterParameters>();
			container.Register<ISymbolReaderProvider, DefaultSymbolReaderProvider>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
			container.Register<AssemblyNameDefinition>();
			container.Register<TypeDefinition>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
			container.Register<MethodReference>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
			container.Register<ParameterDefinition>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
			container.Register<GenericParameter>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
			container.Register<FieldDefinition>();
			container.Register<GenericInstanceMethod>();
			container.Register<TypeReference>(made: Made.Of(FactoryMethod.ConstructorWithResolvableArguments));
			container.Register<GenericInstanceType>();
		}

		public static IResolverContext AddInvocationExpression(this Fake fake, LinqExpression expression, bool addMocks = false)
		{
			var invocationExpression = OnScopedService<IInvocationExpression>(fake,
				new InvocationExpression(fake.Services.Resolve<IMemberVisitorFactory>(),
				expression ?? throw new ArgumentNullException(nameof(expression))));
			var scope = fake.Services.OpenScope(invocationExpression);
			scope.Use(_ => invocationExpression);

			if (addMocks)
			{
				var mocks = OnScopedService<IMockCollection>(fake, new MockCollection());
				scope.Use(_ => mocks);
				fake.Services.RegisterInstance(mocks, serviceKey: invocationExpression);
			}

			return scope;
		}

		private static T OnScopedService<T>(Fake fake, T service)
		{
			// The first check is to avoid hash calculation and typeof call without a need
			return fake.OnScopedServiceRegistration.Count > 0 && fake.OnScopedServiceRegistration.TryGetValue(typeof(T), out var transform) && service != null
				? (T)transform(service)
				: service;
		}
	}
}
