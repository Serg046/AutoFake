using AutoFake.Abstractions;
using DryIoc;
using FluentAssertions;
using Sut;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;
using static AutoFake.FunctionalTests.NewVersionTests;

namespace AutoFake.FunctionalTests;

public class NewVersionTests
{
	[AutoFakeFact]
	public void Test()
	{
		var date = new DateTime(2024, 3, 13);
		var fake = new Fake<SystemUnderTest>();

		var sut = fake.Rewrite(f => f.GetCurrentDate());
		sut.Replace(() => DateTime.Now).Return(date);
		sut.Execute().Should().Be(date);

		var sys = new SystemUnderTest();
		sys.GetCurrentDate().Should().Be(date);
	}

	// to be generated
	public static class FakeContext
	{
		public static readonly AssemblyLoadContext _host;

		static FakeContext()
		{
			_host = new CustomAssemblyLoadContext();
			_host.LoadFromAssemblyPath(Assembly.GetExecutingAssembly().Location);

			var date = new DateTime(2024, 3, 13);
			var fake = new Fake<SystemUnderTest>(
				"C:\\Projects\\GitHub\\AutoFake\\Tests\\AutoFake.FunctionalTests\\NewVersionTests.cs", 26);
			fake.Services.Register<IAssemblyHost, CustomAssemblyHost>(ifAlreadyRegistered: IfAlreadyRegistered.Replace);

			var sut = fake.Rewrite(f => f.GetCurrentDate());
			sut.Replace(() => DateTime.Now).Return(date);
			fake.Rewrite(f => f.GetDateVirtual()).Replace(() => DateTime.Now).Return(date);
			sut.Execute();
			_host.EnterContextualReflection();
		}

		public static void Run(Action action)
		{
			var actType = action.Target.GetType();
			var assembly = _host.Assemblies.Single(a => a.FullName == actType.Assembly.FullName);
			var type = assembly.GetType(actType.FullName);
			var instance = Activator.CreateInstance(type);
			var method = type.GetMethod(action.Method.Name, BindingFlags.Instance | BindingFlags.NonPublic);

			// todo: get rid of that
			using (_host.EnterContextualReflection())
			{
				method.Invoke(instance, null);
			}
		}

		private class CustomAssemblyHost : IAssemblyHost
		{
			public Assembly Load(MemoryStream asmStream, MemoryStream symbolsStream)
			{
				asmStream.Position = symbolsStream.Position = 0;
				return symbolsStream.Length == 0
					? _host.LoadFromStream(asmStream)
					: _host.LoadFromStream(asmStream, symbolsStream);
			}
		}
	}
}

public class CustomAssemblyLoadContext : AssemblyLoadContext
{
	public CustomAssemblyLoadContext() : base("FakeContext", isCollectible: true)
	{
	}

	protected override Assembly Load(AssemblyName assemblyName)
	{
		return base.Load(assemblyName);
	}
}

[XunitTestCaseDiscoverer("AutoFake.FunctionalTests.AutoFakeFactDiscoverer", "AutoFake.FunctionalTests")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AutoFakeFactAttribute : FactAttribute
{

}

public class AutoFakeFactDiscoverer : FactDiscoverer
{
	public AutoFakeFactDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
	{
	}

	protected override IXunitTestCase CreateTestCase(ITestFrameworkDiscoveryOptions discoveryOptions, Xunit.Abstractions.ITestMethod testMethod, IAttributeInfo factAttribute)
	{
		return new AutoFakeXunitTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, testMethodArguments: null);
	}
}

public class AutoFakeXunitTestCase : XunitTestCase
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
	public AutoFakeXunitTestCase()
	{
	}

	internal AutoFakeXunitTestCase(
		IMessageSink diagnosticMessageSink,
		TestMethodDisplay defaultMethodDisplay,
        Xunit.Abstractions.ITestMethod testMethod,
		object[] testMethodArguments)
		: base(diagnosticMessageSink, defaultMethodDisplay, TestMethodDisplayOptions.None, testMethod, testMethodArguments)
	{
	}

	private static Xunit.Abstractions.ITestMethod GetTestMethod(Xunit.Abstractions.ITestMethod testMethod)
	{
		var runtimeMethodInfo = testMethod.Method.ToRuntimeMethod();
		var assembly = FakeContext._host.Assemblies.Single(a => a.FullName == runtimeMethodInfo.DeclaringType.Assembly.FullName);
		var type = assembly.GetType(runtimeMethodInfo.DeclaringType.FullName);
		var method = type.GetMethod(runtimeMethodInfo.Name);

		var typeClass = new TestClass(testMethod.TestClass.TestCollection, new ReflectionTypeInfo(type));
		var methodInfo = new ReflectionMethodInfo(method);
		return new Xunit.Sdk.TestMethod(typeClass, methodInfo);
	}

	public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
	{
		TestMethod = GetTestMethod(TestMethod);
		Method = TestMethod.Method;
		return base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource);
	}
}
