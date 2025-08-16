using Xunit;
using Xunit.Sdk;

namespace AutoFake.xUnit;

[XunitTestCaseDiscoverer("AutoFake.xUnit.AutoFakeFactDiscoverer", "AutoFake.xUnit")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AutoFakeFactAttribute : FactAttribute
{
}