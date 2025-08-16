namespace AutoFake.Abstractions;

public interface IArgValidator
{
    bool Validate(object? argument);
}