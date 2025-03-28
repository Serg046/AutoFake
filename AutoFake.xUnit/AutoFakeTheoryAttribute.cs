using Xunit;
using Xunit.Sdk;

namespace AutoFake.xUnit;

[XunitTestCaseDiscoverer("AutoFake.xUnit.AutoFakeTheoryDiscoverer", "AutoFake.xUnit")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AutoFakeTheoryAttribute : TheoryAttribute
{
    
}