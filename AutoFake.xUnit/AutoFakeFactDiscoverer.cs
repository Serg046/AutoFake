using Xunit.Abstractions;
using Xunit.Sdk;

namespace AutoFake.xUnit;

public class AutoFakeFactDiscoverer : FactDiscoverer
{
    public AutoFakeFactDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
    {
    }

    protected override IXunitTestCase CreateTestCase(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        return new AutoFakeXunitTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, testMethodArguments: null);
    }
}