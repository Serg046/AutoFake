using System.Collections;
using System.Reflection;
using AutoFake.Abstractions;
using AutoFake.Abstractions.Setup;

namespace AutoFake;

public static class ArgValidator
{
    private static readonly string _paramArrayAttributeFullName = typeof(ParamArrayAttribute).FullName!;
    public static IArgValidator Create<T>(Predicate<T> validator) => new ArgValidator<T>(validator);
    public static IArgValidator CreateAny<T>() => new ArgValidator<T>(a => true);
    
    public static bool ValidateArguments(string patchKey, object[] arguments)
    {
        var services = Fake.GetCompositionRoot(Assembly.GetCallingAssembly());
        var patchCollection = services.Resolve<IPatchCollection>();
        var patch = patchCollection.GetPatch(patchKey);
        var parameters = patch.PatchMember.GetParameters();
        if (arguments.Length != patch.Arguments.Length) return false;
        
        for (var i = 0; i < arguments.Length; i++)
        {
            var currentRuntimeArgument = arguments[i];
            var argument = patch.Arguments[i];
            if (IsParamsArgument(i) &&
                currentRuntimeArgument is IEnumerable currentRuntimeEnumerableArgument &&
                argument is IEnumerable enumerableArgument)
            {
                if (!Compare(enumerableArgument, currentRuntimeEnumerableArgument)) return false;
            }
            else if (!Compare(argument, currentRuntimeArgument)) return false;
        }
        
        return true;

        bool IsParamsArgument(int index)
        {
            return index < parameters.Count &&
                   parameters[index].CustomAttributes.Any(p => p.AttributeType.FullName == _paramArrayAttributeFullName);
        }
    }

    private static bool Compare(IEnumerable x, IEnumerable y)
    {
        var enumerator = y.GetEnumerator();
        try
        {
            foreach (var arg in x)
            {
                if (!enumerator.MoveNext() || !Compare(arg, enumerator.Current))
                {
                    return false;
                }
            }

            return true;
        }
        finally
        {
            if (enumerator is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
    
    private static bool Compare(object? x, object? y)
    {
        return x is IArgValidator validator
            ? validator.Validate(y)
            : EqualityComparer<object>.Default.Equals(x, y);
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