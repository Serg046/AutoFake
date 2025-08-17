using Xunit.Abstractions;
using Xunit.Sdk;

namespace AutoFake.xUnit;

public class AutoFakeFactDiscoverer(IMessageSink diagnosticMessageSink) : FactDiscoverer(diagnosticMessageSink)
{
    protected override IXunitTestCase CreateTestCase(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        return new AutoFakeXunitTestCase(DiagnosticMessageSink, discoveryOptions, testMethod);
    }
}