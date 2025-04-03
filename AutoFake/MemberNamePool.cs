using AutoFake.Abstractions;

namespace AutoFake;

internal class MemberNamePool : IMemberNamePool
{
    private readonly Dictionary<string, int> _names = new();

    public string NextFieldName(string baseFieldName)
    {
        if (_names.TryAdd(baseFieldName, 0)) return baseFieldName;

        _names[baseFieldName]++;
        return baseFieldName + _names[baseFieldName];
    }
}