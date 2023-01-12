// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="Type"/> with additional methods.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Returns true of the type is generic, false otherwise.
    /// </summary>
    public static bool IsGenericType(this Type type, Type genericType) => type.IsGenericType && type.GetGenericTypeDefinition() == genericType;
    
    /// <summary>
    /// Returns true of the type is nullable, false otherwise.
    /// </summary>
    public static bool IsNullableType(this Type type) => type.IsGenericType(typeof(Nullable<>));
    
    /// <summary>
    /// Returns the wrapped type of the specified nullable type.
    /// </summary>
    public static Type GetTypeOfNullable(this Type type) => type.GenericTypeArguments[0];
}