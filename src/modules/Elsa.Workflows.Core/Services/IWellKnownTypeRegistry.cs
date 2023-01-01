using Elsa.Extensions;

namespace Elsa.Workflows.Core.Services;

public interface IWellKnownTypeRegistry
{
    void RegisterType(Type type, string alias);
    bool TryGetAlias(Type type, out string alias);
    bool TryGetType(string alias, out Type type);
}

public static class WellKnowTypeRegistryExtensions
{
    public static void RegisterType<T>(this IWellKnownTypeRegistry registry, string alias) => registry.RegisterType(typeof(T), alias);

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

    public static string GetAliasOrDefault(this IWellKnownTypeRegistry registry, Type type) =>
        registry.TryGetAlias(type, out var alias) ? alias : type.GetSimpleAssemblyQualifiedName();
}