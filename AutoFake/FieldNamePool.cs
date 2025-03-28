using AutoFake.Abstractions;

namespace AutoFake;

internal class FieldNamePool : IFieldNamePool
{
    private readonly Dictionary<string, int> _names = new();

    public string NextFieldName(string baseFieldName)
    {
        if (!_names.ContainsKey(baseFieldName))
        {
            _names.Add(baseFieldName, 0);
            return baseFieldName;
        }

        _names[baseFieldName]++;
        return baseFieldName + _names[baseFieldName];
    }
}