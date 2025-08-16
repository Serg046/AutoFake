using System.Reflection;

namespace AutoFake.Abstractions;

public interface IFakeCallback
{
    public void Patch(MethodBase callback);
}