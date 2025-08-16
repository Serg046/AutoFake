using Xunit.Abstractions;
using Xunit.Sdk;

namespace AutoFake.xUnit;

public class AutoFakeTheoryDiscoverer(IMessageSink diagnosticMessageSink) : TheoryDiscoverer(diagnosticMessageSink)
{
    protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod,
        IAttributeInfo theoryAttribute, object[] dataRow)
    {
        return CreateTestCasesForTheory(discoveryOptions, testMethod, theoryAttribute);
    }
    
    protected override IEnumerable<IXunitTestCase> CreateTestCasesForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod,
        IAttributeInfo theoryAttribute)
    {
        return base.CreateTestCasesForTheory(discoveryOptions, AutoFakeFactDiscoverer.PatchTestMethod(testMethod), theoryAttribute);
    }

    protected override IEnumerable<IXunitTestCase> CreateTestCasesForSkip(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod,
        IAttributeInfo theoryAttribute, string skipReason)
    {
        return base.CreateTestCasesForSkip(discoveryOptions, AutoFakeFactDiscoverer.PatchTestMethod(testMethod), theoryAttribute, skipReason);
    }

    protected override IEnumerable<IXunitTestCase> CreateTestCasesForSkippedDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod,
        IAttributeInfo theoryAttribute, object[] dataRow, string skipReason)
    {
        return CreateTestCasesForSkip(discoveryOptions, testMethod, theoryAttribute, skipReason);
    }
}