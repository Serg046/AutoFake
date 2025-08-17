using Xunit.Abstractions;
using Xunit.Sdk;

namespace AutoFake.xUnit;

public class AutoFakeXunitTestCase : XunitTestCase
{
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public AutoFakeXunitTestCase()
    {
    }

    public AutoFakeXunitTestCase(IMessageSink diagnosticMessageSink,
        ITestFrameworkDiscoveryOptions discoveryOptions,
        ITestMethod testMethod)
        : base(diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod)
    {
    }

    public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments,
        ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
    {
        TestMethod = PatchTestMethod(TestMethod);
        Method = TestMethod.Method;
        return await base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource);
    }
    
    public static ITestMethod PatchTestMethod(ITestMethod testMethod)
    {
        var methodInfo = testMethod.Method.ToRuntimeMethod();
        if (methodInfo.DeclaringType?.FullName == null) throw new InvalidOperationException("Cannot find a test method type");
        
        var assembly = Fake.Patch(methodInfo);
        var type = assembly.GetType(methodInfo.DeclaringType.FullName)
                   ?? throw new InvalidOperationException("Cannot find a patched test method type");
        var method = type.GetMethod(methodInfo.Name);
        var testClass = new TestClass(testMethod.TestClass.TestCollection, new ReflectionTypeInfo(type));
        var testMethodInfo = new ReflectionMethodInfo(method);
        return new TestMethod(testClass, testMethodInfo);
    }
}

public class AutoFakeXunitTheoryTestCase : XunitTheoryTestCase
{
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public AutoFakeXunitTheoryTestCase()
    {
    }

    public AutoFakeXunitTheoryTestCase(IMessageSink diagnosticMessageSink,
        ITestFrameworkDiscoveryOptions discoveryOptions,
        ITestMethod testMethod)
        : base(diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod)
    {
    }

    public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments,
        ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
    {
        TestMethod = AutoFakeXunitTestCase.PatchTestMethod(TestMethod);
        Method = TestMethod.Method;
        return await base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource);
    }
}