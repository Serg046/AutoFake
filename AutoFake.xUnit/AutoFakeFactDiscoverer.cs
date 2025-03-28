using Xunit.Abstractions;
using Xunit.Sdk;

namespace AutoFake.xUnit;

public class AutoFakeFactDiscoverer(IMessageSink diagnosticMessageSink) : FactDiscoverer(diagnosticMessageSink)
{
    protected override IXunitTestCase CreateTestCase(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        return base.CreateTestCase(discoveryOptions, PatchTestMethod(testMethod), factAttribute);
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