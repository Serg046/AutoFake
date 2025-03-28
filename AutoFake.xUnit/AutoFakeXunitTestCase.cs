using System.ComponentModel;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AutoFake.xUnit;

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
        ITestMethod testMethod,
        object[]? testMethodArguments)
        : base(diagnosticMessageSink, defaultMethodDisplay, TestMethodDisplayOptions.None, testMethod, testMethodArguments)
    {
    }

    private static ITestMethod GetTestMethod(ITestMethod testMethod)
    {
        var methodInfo = testMethod.Method.ToRuntimeMethod();
        if (methodInfo.DeclaringType?.FullName == null) throw new InvalidOperationException("Cannot find a test method type");
        
        var assembly = Fake.Patch(methodInfo);
        var type = assembly.GetType(methodInfo.DeclaringType.FullName)
            ?? throw new InvalidOperationException("Cannot find a patched test method type");
        var method = type.GetMethod(methodInfo.Name);
        var testClass = new TestClass(testMethod.TestClass.TestCollection, new ReflectionTypeInfo(type));
        var testMethodInfo = new ReflectionMethodInfo(method);
        return new Xunit.Sdk.TestMethod(testClass, testMethodInfo);
    }

    public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
    {
        TestMethod = GetTestMethod(TestMethod);
        Method = TestMethod.Method;
        return base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource);
    }
}