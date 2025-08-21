using Mono.Cecil;

namespace AutoFake;

internal static class Extensions
{
    public static TypeDefinition AsTypeDefinition(this TypeReference type)
        => type as TypeDefinition ?? type.Resolve();

    public static FieldDefinition AsFieldDefinition(this FieldReference field)
        => field as FieldDefinition ?? field.Resolve();

    public static MethodDefinition AsMethodDefinition(this MethodReference method)
        => method as MethodDefinition ?? method.Resolve();
    
    public static string GetClrTypeFullName(this TypeReference type) => type.FullName.Replace('/', '+');

    public static T CreateDefault<T>() => (T)typeof(T).CreateDefault()!;
    
    public static object? CreateDefault(this Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type)! : null;
    }

    public static bool Compare(this MethodReference method1, MethodReference method2)
    {
        return method1 is GenericInstanceMethod
            ? method1.ToString() == method2.ToString()
            : method1 == method2;
    }
}