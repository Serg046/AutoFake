namespace AutoFake;

public static class Arg
{
    public static T IsAny<T>() => Extensions.CreateDefault<T>();
    public static T Is<T>(Predicate<T> validator) => Extensions.CreateDefault<T>();
}