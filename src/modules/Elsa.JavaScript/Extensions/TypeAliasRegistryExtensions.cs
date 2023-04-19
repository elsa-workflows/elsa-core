using Elsa.Extensions;
using Elsa.JavaScript.Contracts;

namespace Elsa.JavaScript.Extensions;

/// <summary>
/// Extends <see cref="ITypeAliasRegistry"/>.
/// </summary>
public static class TypeAliasRegistryExtensions
{
    /// <summary>
    /// Register type <see cref="T"/> with the specified alias.
    /// </summary>
    public static void RegisterType<T>(this ITypeAliasRegistry registry, string alias) => registry.RegisterType(typeof(T), alias);

    /// <summary>
    /// Returns the alias for the specified type. If no alias was found, the assembly qualified type name is returned instead.  
    /// </summary>
    public static string GetAliasOrDefault(this ITypeAliasRegistry registry, Type type, string? defaultName = default) =>
        registry.TryGetAlias(type, out var alias) ? alias : defaultName ?? type.GetSimpleAssemblyQualifiedName();
}