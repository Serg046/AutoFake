using System;

namespace AutoFake
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
    public class ExcludeFromCodeCoverageAttribute : Attribute
    {
    }
}
