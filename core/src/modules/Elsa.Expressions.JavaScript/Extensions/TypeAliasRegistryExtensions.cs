using Elsa.Extensions;
using Elsa.Expressions.JavaScript.Contracts;

namespace Elsa.Expressions.JavaScript.Extensions;

/// <summary>
/// Extends <see cref="ITypeAliasRegistry"/>.
/// </summary>
public static class TypeAliasRegistryExtensions
{
    /// <summary>
    /// Register type <typeparamref name="T"/> with the specified alias.
    /// </summary>
    /// <typeparam name="T">The type to register.</typeparam>
    public static void RegisterType<T>(this ITypeAliasRegistry registry, string alias) => registry.RegisterType(typeof(T), alias);

    /// <summary>
    /// Returns the alias for the specified type. If no alias was found, the assembly qualified type name is returned instead.  
    /// </summary>
    public static string GetAliasOrDefault(this ITypeAliasRegistry registry, Type type, string? defaultName = default) =>
        registry.TryGetAlias(type, out var alias) ? alias : defaultName ?? type.GetSimpleAssemblyQualifiedName();
}