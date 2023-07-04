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

    /// <summary>
    /// Returns true if the specified type is a collection type, false otherwise.
    /// </summary>
    public static bool IsCollectionType(this Type type)
    {
        if (!type.IsGenericType)
            return false;
        
        var elementType = type.GenericTypeArguments[0];
        var collectionType = typeof(ICollection<>).MakeGenericType(elementType);
        var listType = typeof(IList<>).MakeGenericType(elementType);
        return collectionType.IsAssignableFrom(type) || listType.IsAssignableFrom(type);
    }

    /// <summary>
    /// Constructs a collection type from the specified type.
    /// </summary>
    public static Type MakeCollectionType(this Type type) => typeof(ICollection<>).MakeGenericType(type);

    /// <summary>
    /// Returns the element type of the specified collection type.
    /// </summary>
    public static Type GetCollectionElementType(this Type type) => type.GenericTypeArguments[0];
}