using System;
using AutoFake.Expression;
using AutoFake.Setup;
using DryIoc;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake
{
	internal static class ContainerExtensions
	{
		public static void AddServices(this Container container, Type sourceType, FakeOptions fakeOptions)
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
		}

		public static void AddInvocationExpression(this Container container, LinqExpression expression)
		{
            var invocationExpression = new InvocationExpression(expression ?? throw new ArgumentNullException(nameof(expression)));
			container.RegisterDelegate<IInvocationExpression>(() => invocationExpression, Reuse.ScopedTo(expression));
		}
	}
}
