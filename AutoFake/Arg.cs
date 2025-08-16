namespace AutoFake;

public static class Arg
{
    public static T IsAny<T>() => Extensions.CreateDefault<T>();
    public static T Is<T>(Predicate<T> validator) => Extensions.CreateDefault<T>();
    public static IArgValidator Create<T>(Predicate<T> validator) => new ArgValidator<T>(validator); //TODO: make internal
    public static IArgValidator CreateAny<T>() => new ArgValidator<T>(a => true); //TODO: make internal
}

public interface IArgValidator
{
    bool Validate(object? argument);
}

public class ArgValidator<T>(Predicate<T> validator) : IArgValidator
{
    public bool Validate(object? argument)
    {
        return argument == null
            ? !typeof(T).IsValueType
            : argument is T typedArg && validator(typedArg);
    }
}