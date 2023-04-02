using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="Type"/>.
/// </summary>
public static class TypeExtensions
{
    private static readonly ConcurrentDictionary<Type, string> SimpleAssemblyQualifiedTypeNameCache = new();

    /// <summary>
    /// Gets the assembly-qualified name of the type, without any version info etc.
    /// E.g. "System.String, System.Private.CoreLib"
    /// </summary>
    public static string GetSimpleAssemblyQualifiedName(this Type type)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));
        return SimpleAssemblyQualifiedTypeNameCache.GetOrAdd(type, BuildSimplifiedName);
    }

    /// <summary>
    /// Returns the default value for the specified type.
    /// </summary>
    public static object? GetDefaultValue(this Type type) => type.IsClass ? null : Activator.CreateInstance(type);
    
    /// <summary>
    /// Returns the element type of the specified type representing an array or generic enumerable.
    /// </summary>
    public static Type GetEnumerableElementType(this Type type)
    {
        if (type.IsArray)
            return type.GetElementType()!;

        var elementType = FindIEnumerable(type);
        return elementType == null ? type : elementType.GetGenericArguments()[0];
    }

    /// <summary>
    /// Searches for the first implemented IEnumerable interface in the given type hierarchy, and returns the generic type argument of the interface. 
    /// </summary>
    /// <param name="sequenceType">The type to search for the IEnumerable interface.</param>
    /// <returns>The generic type argument of the first implemented IEnumerable interface found in the type hierarchy, or null if none is found.</returns>
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    private static Type? FindIEnumerable([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type? sequenceType)
    {
        if (sequenceType == null || sequenceType == typeof(string))
            return null;
        
        if (sequenceType.IsArray)
            return typeof(IEnumerable<>).MakeGenericType(sequenceType.GetElementType()!);
        
        if (sequenceType.IsGenericType)
        {
            foreach (var arg in sequenceType.GetGenericArguments())
            {
                var enumerable = typeof(IEnumerable<>).MakeGenericType(arg);
                if (enumerable.IsAssignableFrom(sequenceType))
                    return enumerable;
            }
        }
        
        var interfaces = sequenceType.GetInterfaces();
        
        if (interfaces is { Length: > 0 })
        {
            foreach (var interfaceType in interfaces)
            {
                var enumerable = FindIEnumerable(interfaceType);
                if (enumerable != null) return enumerable;
            }
        }
        if (sequenceType.BaseType != null && sequenceType.BaseType != typeof(object))
            return FindIEnumerable(sequenceType.BaseType);
        
        return null;
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

            var genericArguments = type.GetGenericArguments();
            var simplifiedGenericArguments = genericArguments.Select(BuildSimplifiedName);

            return $"{typeNameWithoutArity}{arity}[[{string.Join("],[", simplifiedGenericArguments)}]], {assemblyName}";
        }

        var typeName = type.FullName;
        return $"{typeName}, {assemblyName}";
    }
}