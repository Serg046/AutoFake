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
}