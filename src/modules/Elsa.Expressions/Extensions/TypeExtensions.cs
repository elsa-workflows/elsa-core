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
        if (type == null) throw new ArgumentNullException(nameof(type));

        return SimpleAssemblyQualifiedTypeNameCache.GetOrAdd(type, GetSimpleAssemblyQualifiedNameInternal);
    }

    private static string GetSimpleAssemblyQualifiedNameInternal(Type type) => $"{type.FullName}, {Assembly.GetAssembly(type)!.GetName().Name}";
    
    /// <summary>
    /// Returns the default value for the specified type.
    /// </summary>
    public static object? GetDefaultValue(this Type type) => type.IsClass ? null : Activator.CreateInstance(type);
}