namespace AutoFake.Abstractions;

internal interface IMemberNamePool
{
    string NextFieldName(string baseFieldName);
}