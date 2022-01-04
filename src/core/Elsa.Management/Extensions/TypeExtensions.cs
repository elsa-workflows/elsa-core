namespace Elsa.Management.Extensions;

public static class TypeExtensions
{
    public static bool IsGenericType(this Type type, Type genericType) => type.IsGenericType && type.GetGenericTypeDefinition() == genericType;
    public static bool IsNullableType(this Type type) => type.IsGenericType(typeof(Nullable<>));
    public static Type GetTypeOfNullable(this Type type) => type.GenericTypeArguments[0];
}