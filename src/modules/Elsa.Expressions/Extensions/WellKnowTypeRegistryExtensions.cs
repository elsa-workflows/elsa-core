using Elsa.Expressions.Contracts;
using Elsa.Extensions;

namespace Elsa.Expressions.Extensions;

/// <summary>
/// Extends <see cref="IWellKnownTypeRegistry"/>.
/// </summary>
public static class WellKnowTypeRegistryExtensions
{
    /// <summary>
    /// Register type <see cref="T"/> with the specified alias.
    /// </summary>
    public static void RegisterType<T>(this IWellKnownTypeRegistry registry, string alias) => registry.RegisterType(typeof(T), alias);

    /// <summary>
    /// Attempt to return a type with the specified alias.
    /// </summary>
    public static bool TryGetTypeOrDefault(this IWellKnownTypeRegistry registry, string alias, out Type type)
    {
        if (registry.TryGetType(alias, out type))
            return true;

        var t = Type.GetType(alias);

        if (t == null)
            return false;

        type = t;
        return true;
    }

    /// <summary>
    /// Returns the alias for the specified type. If no alias was found, the assembly qualified type name is returned instead.  
    /// </summary>
    public static string GetAliasOrDefault(this IWellKnownTypeRegistry registry, Type type) =>
        registry.TryGetAlias(type, out var alias) ? alias : type.GetSimpleAssemblyQualifiedName();
    
    /// <summary>
    /// Returns the type associated with the specified alias. If no type was found, the alias is interpreted as a type name/
    /// </summary>
    public static Type GetTypeOrDefault(this IWellKnownTypeRegistry registry, string alias) => registry.TryGetType(alias, out var type) ? type : Type.GetType(alias)!;
}