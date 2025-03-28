namespace AutoFake.Abstractions;

internal interface IFieldNamePool
{
    string NextFieldName(string baseFieldName);
}