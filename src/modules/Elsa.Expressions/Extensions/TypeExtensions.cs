using System.Collections.Concurrent;
using System.Reflection;

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