namespace Elsa.Workflows.Core.Helpers;

public static class TypeNameHelper
{
    public static string GenerateTypeName<T>() => GenerateTypeName(typeof(T));
    public static string GenerateTypeName(Type type) => type.FullName!;
}