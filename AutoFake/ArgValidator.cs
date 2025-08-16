using System.Reflection;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;

namespace AutoFake;

public static class ArgValidator
{
    public static IArgValidator Create<T>(Predicate<T> validator) => new ArgValidator<T>(validator);
    public static IArgValidator CreateAny<T>() => new ArgValidator<T>(a => true);
    
    public static bool ValidateArguments(string patchKey, object[] arguments)
    {
        var services = Fake.GetCompositionRoot(Assembly.GetCallingAssembly());
        var patchCollection = services.Resolve<IPatchCollection>();
        var patch = patchCollection.GetPatch(patchKey);
        return patch.Arguments.SequenceEqual(arguments, new ArgumentComparer());
    }
    
    private class ArgumentComparer : IEqualityComparer<object>
    {
        bool IEqualityComparer<object>.Equals(object? x, object? y)
        {
            return x is IArgValidator validator
                ? validator.Validate(y)
                : EqualityComparer<object>.Default.Equals(x, y);
        }

        int IEqualityComparer<object>.GetHashCode(object obj) => EqualityComparer<object>.Default.GetHashCode(obj);
    }
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