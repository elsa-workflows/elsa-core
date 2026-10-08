using System.Collections.Concurrent;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="Type"/> with additional methods.
/// </summary>
public static class TypeExtensions
{
    private static readonly ConcurrentDictionary<Type, string> SimpleAssemblyQualifiedTypeNameCache = new();

    /// <summary>
    /// Gets the assembly-qualified name of the type, without version, culture, and public key token information.
    /// </summary>
    public static string GetSimpleAssemblyQualifiedName(this Type type)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));
        return SimpleAssemblyQualifiedTypeNameCache.GetOrAdd(type, BuildSimplifiedName);
    }

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

    /// <summary>
    /// Determines whether the specified type is a numeric type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the specified type is numeric, otherwise false.</returns>
    public static bool IsNumericType(this Type type)
    {
        return type.IsPrimitive || type == typeof(decimal) || type == typeof(float) || type == typeof(double) || type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte);
    } 

    private static string BuildSimplifiedName(Type type)
    {
        var assemblyName = type.Assembly.GetName().Name;

        if (type.IsGenericType)
        {
            var genericTypeName = type.GetGenericTypeDefinition().FullName!;
            var backtickIndex = genericTypeName.IndexOf('`');
            var typeNameWithoutArity = genericTypeName[..backtickIndex];
            var arity = genericTypeName[backtickIndex..];
            var simplifiedGenericArguments = type.GetGenericArguments().Select(BuildSimplifiedName);

            return $"{typeNameWithoutArity}{arity}[[{string.Join("],[", simplifiedGenericArguments)}]], {assemblyName}";
        }

        return $"{type.FullName}, {assemblyName}";
    }
}
